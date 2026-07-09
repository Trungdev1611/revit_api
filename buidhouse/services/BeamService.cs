using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Simpleform.buidhouse.constant;
using Simpleform.buidhouse.models;
using Simpleform.buidhouse.utils;
using Simpleform.drawWallRefactor;

namespace Simpleform.buidhouse.services;

public class BeamService
{
    private readonly Document _doc;
    private const string BeamFamilyNameRectangle = "Concrete Rectangular Beam";
    private const string BeamFamilyNameSquare = "Concrete Square Beam";

    public BeamService(Document doc)
    {
        _doc = doc;
    }

    public void LoadFamilyBeam(bool isSquare)
    {
        string relativePath = Constant.GetBeamFamilyFile(isSquare);
        FamilyPathUtil.TryLoadFamily(_doc, relativePath, out _);
    }

    public FamilyInstance? CreateBeam(BeamConfig config)
    {
        //get level
        Level? level = _doc.GetFirstItemOrCondition<Level>(
            x => x.Name == config.levelName);

        if (level == null)
        {
            TaskDialog.Show("Error", $"Level not found: {config.levelName}");
            return null;
        }

        //get family beam - rectangle/ square
        var shapeBeam = config.width == config.height ? BeamFamilyNameSquare : BeamFamilyNameRectangle;
        var BeamFamilyName = $"{config.width}x{config.height}mm";

        //check xem loại dầm đó tồn tại trong dự án chưa
        FamilySymbol? beamTypeSymbol = _doc.GetFirstItemOrCondition<FamilySymbol>(
            x =>
                x.FamilyName == shapeBeam &&
                x.Name == BeamFamilyName,
            BuiltInCategory.OST_StructuralFraming);

        if (beamTypeSymbol == null)
        {
            FamilySymbol? baseSymbol = _doc.GetFirstItemOrCondition<FamilySymbol>(
                x => x.FamilyName == shapeBeam,
                BuiltInCategory.OST_StructuralFraming);

            if (baseSymbol == null)
            {
                bool isSquare = config.width == config.height;
                if (FamilyPathUtil.TryLoadFamily(_doc, Constant.GetBeamFamilyFile(isSquare), out Family? family))
                {
                    ElementId? firstSymbolId = family.GetFamilySymbolIds().FirstOrDefault();
                    if (firstSymbolId != null && firstSymbolId != ElementId.InvalidElementId)
                    {
                        baseSymbol = _doc.GetElement(firstSymbolId) as FamilySymbol;
                    }
                }
            }

            if (baseSymbol == null)
            {
                TaskDialog.Show("Error", $"Beam family not found: {shapeBeam}");
                return null;
            }

            beamTypeSymbol = baseSymbol.Duplicate(BeamFamilyName) as FamilySymbol;

            if (beamTypeSymbol == null)
            {
                TaskDialog.Show("Error", $"Cannot duplicate beam type: {BeamFamilyName}");
                return null;
            }

            SetParameterIfExists(beamTypeSymbol, "b", config.width);
            SetParameterIfExists(beamTypeSymbol, "h", config.height);
        }

        if (!beamTypeSymbol.IsActive)
        {
            beamTypeSymbol.Activate();
            _doc.Regenerate();
        }

        Line line = Line.CreateBound(config.StartPoint, config.EndPoint);

        FamilyInstance beam = _doc.Create.NewFamilyInstance(
            line,
            beamTypeSymbol,
            level,
            StructuralType.Beam);

        return beam;
    }

    private static void SetParameterIfExists(Element element, string parameterName, double value)
    {
        Parameter? parameter = element.LookupParameter(parameterName);

        if (parameter == null || parameter.IsReadOnly)
        {
            return;
        }

        parameter.Set(value);

    }
}