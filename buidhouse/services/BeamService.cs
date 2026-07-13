using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using BuildHouse.Utils;
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
        var familyName = config.width == config.height ? BeamFamilyNameSquare : BeamFamilyNameRectangle;
        var TypeBeamName = $"{config.width}x{config.height}mm";

        //check xem loại dầm đó tồn tại trong dự án chưa
        FamilySymbol? beamTypeSymbol = _doc.GetFirstItemOrCondition<FamilySymbol>(
            x =>
                x.FamilyName == familyName &&
                x.Name == TypeBeamName,
            BuiltInCategory.OST_StructuralFraming);


        string typeBeam = config.width == config.height ? "square" : "rectangular"; 
        bool isExistFamilyNameInDictionary = Constant.BeamFamilyFiles.TryGetValue(typeBeam, out string relativePath);

        //nếu không tồn tại loại dầm đó trong project, xem family dầm vuông hay chữ nhật có tồn tại trong project không để dùng nhân bản
        // if (beamTypeSymbol == null)
        // {
        //     FamilySymbol? baseSymbol = _doc.GetFirstItemOrCondition<FamilySymbol>(
        //         x => x.FamilyName == shapeBeam,
        //         BuiltInCategory.OST_StructuralFraming);

            //nếu vẫn không tồn tại family dầm vuông hay chữ nhật => load mới family
            // if (baseSymbol == null)
            // {
                //check xem dầm vuông hay chữ nhật để load  family tương ứng
                
                //check xem tồn tại key trong dictionary không và lấy ra relativePath
               
            //     //từ relativePath lấy ra absolute path
            //     string absolutePath = RevitUtil.resolveFamilyPath(relativePath);

            // //load thử family và trả đầu ra là family đã được load
            //     if (FamilyUtilClass.TryLoadFamily(_doc, absolutePath, out Family? familyloaded))
            //     {
            //         //lấy ra id của symbol đầu tiên trong family đã được load
            //         ElementId? firstSymbolId = familyloaded.GetFamilySymbolIds().FirstOrDefault();
            //         if (firstSymbolId != null && firstSymbolId != ElementId.InvalidElementId)
            //         {
            //             baseSymbol = _doc.GetElement(firstSymbolId) as FamilySymbol;
            //         }
            //     }

            // }
            Family familyloaded = FamilyUtilClass.GetFamilyIfExistedOrloadNew(_doc, familyName, relativePath);
            //lấy symbol (type) dầu tiên tìm được
            ElementId? firstSymbolId = familyloaded.GetFamilySymbolIds().FirstOrDefault();

            FamilySymbol baseSymbol = null;
            if (firstSymbolId != null && firstSymbolId != ElementId.InvalidElementId)
                    {
                        baseSymbol = _doc.GetElement(firstSymbolId) as FamilySymbol;
                    }
            //nếu vẫn khoogn có type nào để nhân bản => show error
            if (baseSymbol == null)
            {
                TaskDialog.Show("Error", $"Beam family don't have any type");
                return null;
            }

            //có type để nhân bản => nhân bản type đó với name là TypeBeamName
            beamTypeSymbol = baseSymbol.Duplicate(TypeBeamName) as FamilySymbol;

            if (beamTypeSymbol == null)
            {
                TaskDialog.Show("Error", $"Cannot duplicate beam type: {TypeBeamName}");
                return null;
            }

            //đặt height và width cho type đã nhân bản
            SetParameterIfExists(beamTypeSymbol, "b", config.width);
            SetParameterIfExists(beamTypeSymbol, "h", config.height);
        // }

        //bắt buộc phải active type đã nhân bản để sử dụng được
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