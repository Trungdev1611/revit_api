using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using BuildHouse.Utils;
using Simpleform.buidhouse.models;
using Simpleform.buidhouse.utils;
using Simpleform.buidhouse.utils;
using Simpleform.drawWallRefactor;

namespace Simpleform.buidhouse.services;

public class BeamService
{
    private readonly Document _doc;
    private const string BeamFamilyNameRectangle = "Concrete - Rectangular Beam";
    private const string BeamFamilyNameSquare = "Concrete Square Beam";

    public BeamService(Document doc)
    {
        _doc = doc;
    }

    public FamilyInstance? CreateBeam(BeamConfig config, Level level, XYZ startPoint, XYZ endPoint)
    {
        bool isSquare = Math.Abs(config.Width - config.Height) < 1e-6;
        string familyName = isSquare ? BeamFamilyNameSquare : BeamFamilyNameRectangle;
        // Đổi suffix để tránh type cũ bị set nhầm feet (220ft x 400ft)
        string typeBeamName = $"{config.Width}x{config.Height}";
        string familyKey = isSquare ? "beam-square" : "beam-rectangular";

        double bInternal = RevitUtil.convertToMeter(config.Width);
        double hInternal = RevitUtil.convertToMeter(config.Height);

        Family? familyloaded = FamilyUtilClass.GetFamilyIfExistedOrloadNew(_doc, familyName, familyKey);
        if (familyloaded == null)
        {
            return null;
        }

        familyName = familyloaded.Name;

        FamilySymbol? beamTypeSymbol = _doc.GetFirstItemOrCondition<FamilySymbol>(
            x => x.Family.Id == familyloaded.Id && x.Name == typeBeamName,
            BuiltInCategory.OST_StructuralFraming,
            isType: true);

        if (beamTypeSymbol == null)
        {
            ElementId firstSymbolId = familyloaded.GetFamilySymbolIds().FirstOrDefault();
            if (firstSymbolId == null || firstSymbolId == ElementId.InvalidElementId)
            {
                TaskDialog.Show("Error", "Beam family don't have any type");
                return null;
            }

            FamilySymbol? baseSymbol = _doc.GetElement(firstSymbolId) as FamilySymbol;
            if (baseSymbol == null)
            {
                TaskDialog.Show("Error", "Beam family don't have any type");
                return null;
            }

            if (!baseSymbol.IsActive)
            {
                baseSymbol.Activate();
                _doc.Regenerate();
            }

            beamTypeSymbol = baseSymbol.Duplicate(typeBeamName) as FamilySymbol;
            if (beamTypeSymbol == null)
            {
                TaskDialog.Show("Error", $"Cannot duplicate beam type: {typeBeamName}");
                return null;
            }
        }

        // Luôn set lại kích thước (mm→feet) — tránh type cũ bị set nhầm đơn vị
        SetParameterIfExists(beamTypeSymbol, "b", bInternal);
        SetParameterIfExists(beamTypeSymbol, "h", hInternal);
        AppLog.Information(
            "Beam type '{0}' b={1:F4}ft h={2:F4}ft (from {3}x{4}mm)",
            beamTypeSymbol.Name, bInternal, hInternal, config.Width, config.Height);

        if (!beamTypeSymbol.IsActive)
        {
            beamTypeSymbol.Activate();
            _doc.Regenerate();
        }

        XYZ start = new XYZ(startPoint.X, startPoint.Y, level.Elevation);
        XYZ end = new XYZ(endPoint.X, endPoint.Y, level.Elevation);
        if (start.DistanceTo(end) < 1e-9)
        {
            AppLog.Warning("Skip beam: zero-length line");
            return null;
        }

        Line line = Line.CreateBound(start, end);
        FamilyInstance newBeam = _doc.Create.NewFamilyInstance(
            line,
            beamTypeSymbol,
            level,
            StructuralType.Beam);

        AppLog.Information(
            "Beam created id={0} type={1} z={2:F3} len={3:F3}",
            newBeam?.Id.IntegerValue,
            beamTypeSymbol.Name,
            level.Elevation,
            start.DistanceTo(end));


        newBeam?.SetMarkAndComment(config.Mark, config.Comment);
        return newBeam;
    }

    private static void SetParameterIfExists(Element element, string parameterName, double valueInternal)
    {
        Parameter? parameter = element.LookupParameter(parameterName);
        if (parameter == null || parameter.IsReadOnly)
        {
            return;
        }

        parameter.Set(valueInternal);
    }
}
