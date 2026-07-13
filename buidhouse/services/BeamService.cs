using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using BuildHouse.Utils;
using Simpleform.buidhouse.models;
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

    public FamilyInstance? CreateBeam(BeamConfig config)
    {
        Level? level = _doc.GetFirstItemOrCondition<Level>(
            x => x.Name == config.levelName);

        if (level == null)
        {
            TaskDialog.Show("Error", $"Level not found: {config.levelName}");
            return null;
        }

        bool isSquare = config.width == config.height;
        string familyName = isSquare ? BeamFamilyNameSquare : BeamFamilyNameRectangle;
        string typeBeamName = $"{config.width}x{config.height}mm";
        string familyKey = isSquare ? "beam-square" : "beam-rectangular";

        //check xem loại dầm đó tồn tại trong dự án chưa
        FamilySymbol? beamTypeSymbol = _doc.GetFirstItemOrCondition<FamilySymbol>(
            x =>
                x.FamilyName == familyName &&
                x.Name == typeBeamName,
            BuiltInCategory.OST_StructuralFraming);

        if (beamTypeSymbol == null)
        {
            Family? familyloaded = FamilyUtilClass.GetFamilyIfExistedOrloadNew(_doc, familyName, familyKey);
            if (familyloaded == null)
            {
                return null;
            }

            ElementId? firstSymbolId = familyloaded.GetFamilySymbolIds().FirstOrDefault();
            FamilySymbol? baseSymbol = null;
            if (firstSymbolId != null && firstSymbolId != ElementId.InvalidElementId)
            {
                baseSymbol = _doc.GetElement(firstSymbolId) as FamilySymbol;
            }

            if (baseSymbol == null)
            {
                TaskDialog.Show("Error", "Beam family don't have any type");
                return null;
            }

            beamTypeSymbol = baseSymbol.Duplicate(typeBeamName) as FamilySymbol;
            if (beamTypeSymbol == null)
            {
                TaskDialog.Show("Error", $"Cannot duplicate beam type: {typeBeamName}");
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

        return _doc.Create.NewFamilyInstance(
            line,
            beamTypeSymbol,
            level,
            StructuralType.Beam);
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
