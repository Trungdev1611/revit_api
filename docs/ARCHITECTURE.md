# Quick House Builder — Giải thích kiến trúc (masterv2)

## Tổng quan luồng chạy

```
User bấm command trong Revit
    → BuildHouseCommand (IExternalCommand)
        → PickPoint() lấy tâm nhà
        → Transaction.Start()
            → GridService.createGrids()           // vẽ trục 1,2,A,B
            → LevelService.CreateOrUpdateLevel()    // Level 1,2,3
            → FloorService.getFloorTypeIdOrCreateNew()
            → GridService.createFootprintLoop(100)  // biên sàn đua 100mm
            → FloorService.createBlindingConcrete()
            → FloorService.setOffsetFromInInitialPosition(-150mm)
        → Transaction.Commit()
```

## Cấu trúc thư mục

| Thư mục | Vai trò |
|---------|---------|
| `commands/` | Entry point — nhận lệnh từ Revit, bọc Transaction |
| `services/` | Logic nghiệp vụ: grid, level, floor, column |
| `models/` | Config / DTO (HouseConfig, ColumnConfig, …) |
| `utils/` | Helper dùng chung (đổi đơn vị, CurveLoop, path DLL) |
| `constant/` | Đường dẫn file .rfa tương đối |
| `families/` | File .rfa đóng gói cùng add-in |
| `buildhouse.addin` | Manifest — Revit biết load DLL và class nào |

## Deploy sau build

```
bin/Release/net48/
├── buildhouse.addin              ← copy vào Revit Addins/2023/
└── buildhouse_Files/
    ├── Simpleform.dll
    └── families/Columns/*.rfa
```

## Revit API patterns dùng trong project

### Transaction
Mọi thay đổi model phải nằm trong `Transaction`. Commit = lưu; RollBack = hủy.

### Type vs Instance
- `FloorType`, `FamilySymbol` = Type (khuôn)
- `Floor`, `FamilyInstance` = Instance (đối tượng trên model)
- Filter Type: `isType: true` trong `GetFirstItemOrCondition`

### Đơn vị
Revit API dùng **feet** nội bộ. UI hiển thị mm. Dùng `UnitUtils` / `RevitUtil.convertToMeter(mm)`.

### CurveLoop cho sàn
4 cạnh nối đuôi đầu (khác với 4 đường Grid). Offset biên bằng `edgeExtensionMm` trong `getFootprintBounds`.

### Load Family
`doc.LoadFamily(pathToRfa)` — path phải là **file .rfa**, không phải folder.
Path resolve từ thư mục chứa DLL: `RevitUtil.resolveFamilyPath(...)`.

## Các service chính

### GridService
- `createGrids()`: 2 trục dọc (1,2) + 2 trục ngang (A,B), nhô thêm 2000mm hai đầu
- `createFootprintLoop(extensionMm)`: hình chữ nhật khép kín cho sàn
- `getFootprintBounds()`: tính left/right/bottom/top từ tâm ± 2500/3000mm

### LevelService
- Tìm Level theo tên — có thì sửa Elevation, chưa có thì `Level.Create`

### FloorService
- Tìm `FloorType` (isType: true) theo tên + độ dày
- Duplicate + chỉnh `CompoundStructure.SetLayerWidth` nếu cần
- `Floor.Create(curveLoops, floorTypeId, levelId)`
- Offset sàn: `FLOOR_HEIGHTABOVELEVEL_PARAM`

### ColumnService
- Tìm `FamilySymbol` trong project
- Không có → `LoadFamily` từ `families/Columns/*.rfa`
- Duplicate type mới → `Activate()` → `NewFamilyInstance`

## File .addin

| Thẻ | Ý nghĩa |
|-----|---------|
| Assembly | Đường dẫn DLL (tương đối so với .addin) |
| FullClassName | Namespace + class implement IExternalCommand |
| ClientId | GUID duy nhất |
