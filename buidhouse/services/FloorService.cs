
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

        string duplicateName = $"{floorConfig.FloortypeName}-new";
        var existingDuplicate = _doc.GetFirstItemOrCondition<FloorType>(
            floorType => floorType.Name == duplicateName,
            isType: true);
        if (existingDuplicate != null && matchesThickness(existingDuplicate)) {
            return existingDuplicate.Id;
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

        FloorType newFloorType = existingDuplicate
            ?? sourceFloorType.Duplicate(duplicateName) as FloorType;

        CompoundStructure compound = newFloorType.GetCompoundStructure();
        setTotalThickness(compound, floorConfig.Thickness);
        newFloorType.SetCompoundStructure(compound);
        return newFloorType.Id;
    }

    private static void setTotalThickness(CompoundStructure compound, double targetThickness) {
        if (compound.LayerCount == 1) {
            compound.SetLayerWidth(0, targetThickness);
            return;
        }

        IList<CompoundStructureLayer> layers = compound.GetLayers();
        int structureLayerIndex = 0;
        for (int i = 0; i < layers.Count; i++) {
            if (layers[i].Function == MaterialFunctionAssignment.Structure) {
                structureLayerIndex = i;
                break;
            }
        }

        double otherLayersWidth = 0;
        for (int i = 0; i < compound.LayerCount; i++) {
            if (i != structureLayerIndex) {
                otherLayersWidth += compound.GetLayerWidth(i);
            }
        }

        compound.SetLayerWidth(structureLayerIndex, targetThickness - otherLayersWidth);
    }

    public Floor createBlindingConcrete(IList<CurveLoop> curveLoops, BlindingConcreteConfig blindingConcreteConfig, ElementId floorTypeId, ElementId levelId) {
        Floor newFloor = Floor.Create(_doc, curveLoops, floorTypeId, levelId);
        return newFloor;
    }

    public bool setOffsetFromInInitialPosition(Floor floorTarget, double valueOffset) {
        if(floorTarget == null) {
            return false;
        }
        Parameter offsetParam = floorTarget.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
        return offsetParam.Set(valueOffset);
     
    }

}
