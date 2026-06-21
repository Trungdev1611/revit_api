using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Simpleform.drawWallRefactor;

namespace Simpleform.drawWallOnLine;

[Transaction(TransactionMode.Manual)]
public class Main:BaseClass
{
    protected override Result ExcuteInside(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        //selection filter
        ISelectionFilter lineSelecFilter = new SelectionDraw(uidoc);
        
        //required user pick selection
        IList<Reference> pickRefs = uidoc.Selection.PickObjects(
            ObjectType.Element,
            lineSelecFilter, 
            "Hãy quét chọn vùng chứa các đường Line/Arc để tự động vẽ tường");
        if ( pickRefs == null ||  pickRefs.Count == 0)
        {
            message = "Khong co phan tu nao duoc chon";
            return Result.Cancelled;
        }
        
        //get FirstlevelId in revit
        ElementId firstLevelId = doc.getFirstLevelInView();
        
        //get default wallType
        WallType wallTypeDefault = doc.GetWallTypeDefault();
        
        //get info parameters
        Level level = doc.GetElement(firstLevelId) as Level;
        FamilySymbol doorTypeDefault = doc.GetFirstItemOrCondition<FamilySymbol>(
            category: BuiltInCategory.OST_Doors, isType:true
        );
        using Transaction t = new Transaction(doc, "drawWallOnTheExistLine");
        {
            t.Start();
            foreach (var item in pickRefs)
            {
                CurveElement? curveElement = doc.GetElement(item) as CurveElement;
                if (curveElement != null)
                {
                    //draw wall Instance
                    Curve curve = curveElement.GeometryCurve;
                    Wall newWall = Wall.Create(doc, curve,  wallTypeDefault.Id, firstLevelId, 10, 0, false, false);
                    
                    // 2. Tính toán vị trí đặt cửa (Ví dụ: Trung điểm của bức tường vừa vẽ)
                    XYZ doorLocation = curve.Evaluate(0.5, true);
                    
                
                    //draw door in each wall
                    DoorFunction.AddDoor(doc, doorLocation, newWall, level, doorTypeDefault);
                }
            }

            TaskDialog.Show("Thông báo", $"Name of DoorType ${doorTypeDefault.Name}");
            t.Commit();
        }
        return Result.Succeeded;
        
    }
}