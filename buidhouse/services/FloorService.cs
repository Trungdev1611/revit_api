
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Simpleform.buidhouse.Interface;
using Simpleform.buidhouse.models;
using Simpleform.drawWallRefactor;

namespace Simpleform.buidhouse.services;

public class FloorService
{
    private readonly Document _doc;
    
    public FloorService(Document doc) {
        this._doc = doc;
    }

    public ElementId getFloorTypeIdOrCreateNew(IFloor floorConfig) {

        Func<FloorType, bool> conditionName = floorType => 
        floorType.Name == floorConfig.FloortypeName; //check name

        Func<FloorType, bool> conditionThickness = floorType => 
        floorType.GetCompoundStructure().GetWidth() == floorConfig.Thickness; //check thickness
        
        //check if floor type with name and thickness exists
        var existingFloorTypes = _doc.GetFirstItemOrCondition<FloorType>(floorType => conditionName(floorType) && conditionThickness(floorType));
        if(existingFloorTypes != null) {
            return existingFloorTypes.Id;
        }

        //check if floor type with name exists or get first item
        var exitsfloorTypeWithName = _doc.GetFirstItemOrCondition<FloorType>(conditionName);
        if(exitsfloorTypeWithName != null) {
          FloorType newFloorTypeDuplicate =  exitsfloorTypeWithName.Duplicate($"{floorConfig.FloortypeName}-new") as FloorType;
            //create new structure with thickness - 1 layer
            var newStructure = CompoundStructure
            .CreateSingleLayerCompoundStructure( 
                MaterialFunctionAssignment.Structure,
                 floorConfig.Thickness,
                  ElementId.InvalidElementId);

            newFloorTypeDuplicate.SetCompoundStructure(newStructure);
            return newFloorTypeDuplicate.Id;
   
        }
        TaskDialog.Show("Thong bao", "Không tìm thấy kiểu sàn nào trong dự án!");

        return null;

    }

    public Floor createBlindingConcrete(IList<CurveLoop> curveLoops, BlindingConcreteConfig blindingConcreteConfig, ElementId floorTypeId, ElementId levelId) {
        Floor newFloor = Floor.Create(_doc, curveLoops, floorTypeId, levelId);
        return newFloor;
    }

}