
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
        const double thicknessTolerance = 1e-6;

        bool matchesName(FloorType floorType) =>
            floorType.Name == floorConfig.FloortypeName;

        bool matchesThickness(FloorType floorType) =>
            Math.Abs(floorType.GetCompoundStructure().GetWidth() - floorConfig.Thickness) < thicknessTolerance;

        var existingFloorType = _doc.GetFirstItemOrCondition<FloorType>(
            floorType => matchesName(floorType) && matchesThickness(floorType),
            isType: true);
        if (existingFloorType != null) {
            return existingFloorType.Id;
        }

        var sourceFloorType = _doc.GetFirstItemOrCondition<FloorType>(matchesName, isType: true)
            ?? _doc.GetFirstItemOrCondition<FloorType>(isType: true);

        if (sourceFloorType == null) {
            TaskDialog.Show("Thong bao", "Không tìm thấy kiểu sàn nào trong dự án!");
            return null;
        }

        if (matchesThickness(sourceFloorType)) {
            return sourceFloorType.Id;
        }

        FloorType newFloorType = sourceFloorType.Duplicate($"{floorConfig.FloortypeName}-new") as FloorType;
        ElementId materialId = sourceFloorType.GetCompoundStructure().GetMaterialId(0);

        var layers = new List<CompoundStructureLayer>
        {
            new CompoundStructureLayer(
                floorConfig.Thickness,
                MaterialFunctionAssignment.Structure,
                materialId)
        };

        // Floor chỉ nhận simple compound structure, không dùng CreateSingleLayerCompoundStructure
        CompoundStructure newStructure = CompoundStructure.CreateSimpleCompoundStructure(layers);
        newFloorType.SetCompoundStructure(newStructure);
        return newFloorType.Id;
    }

    public Floor createBlindingConcrete(IList<CurveLoop> curveLoops, BlindingConcreteConfig blindingConcreteConfig, ElementId floorTypeId, ElementId levelId) {
        Floor newFloor = Floor.Create(_doc, curveLoops, floorTypeId, levelId);
        return newFloor;
    }

}