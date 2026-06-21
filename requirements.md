# ĐỀ BÀI: ADD-IN "QUICK HOUSE BUILDER" (GIAI ĐOẠN 1)

## 1. Mô tả bài toán
Viết một External Command trong Revit API (C#) cho phép người dùng kích chọn 1 điểm bất kỳ trên mặt bằng tầng 1 (`Level 1`). Từ điểm đó, hệ thống sẽ tự động tính toán, dựng hệ lưới trục và xây dựng một ngôi nhà dạng khối hộp cơ bản bao gồm: Hệ lưới → Cột → Dầm → Tường bao → Sàn.

---

## 2. Thông số đầu vào (Tạm thời gán cứng - Hardcoded)
Khai báo các thông số sau thành các biến toàn cục (hoặc gom vào một Class Model đặt tên là `HouseConfig`) để sau này tiện kết nối với giao diện WPF:

* **Đơn vị nhập vào:** mm (Trong code phải quy đổi sang Feet trước khi truyền vào Revit API).
* **Kích thước nhà:** 1 gian kích thước $5000 \times 6000 \text{ mm}$ (Rộng theo phương X: 5m, Dài theo phương Y: 6m).
* **Số tầng:** 2 tầng (Tầng 1 cao $3600 \text{ mm}$, Tầng 2 cao $3300 \text{ mm}$).
* **Tiết diện cấu kiện (Đảm bảo có sẵn Type này trong file Revit mẫu):**
    * Cột vuông bê tông: `"300x300mm"`
    * Dầm bê tông: `"300x500mm"`
    * Tường kiến trúc: `"Generic - 200mm"`
    * Sàn bê tông: `"Generic - 150mm"`

---

## 3. Yêu cầu kỹ thuật chi tiết (Các bước thực hiện)

### Bước 1: Tiếp nhận điểm gốc (Origin Point)
* Sử dụng hàm `Selection.PickPoint` để yêu cầu người dùng chọn vị trí click trên màn hình.
* Lấy tọa độ điểm này làm gốc định vị $P_0(X_0, Y_0, Z_0)$.

### Bước 2: Tạo hệ lưới trục (Grids)
Từ điểm $P_0$, vẽ 4 đường Grid tạo thành một ô vuông $5\text{m} \times 6\text{m}$:
* **Phương X (Trục dọc định vị phương X):**
    * Trục 1: Đi qua tọa độ $X_0$.
    * Trục 2: Cách Trục 1 một khoảng $5000 \text{ mm}$ (Tọa độ $X_0 + 5000$).
* **Phương Y (Trục ngang định vị phương Y):**
    * Trục A: Đi qua tọa độ $Y_0$.
    * Trục B: Cách Trục A một khoảng $6000 \text{ mm}$ (Tọa độ $Y_0 + 6000$).
* *Yêu cầu phụ:* Sử dụng Parameter đổi tên (Name) của các đường Grid này lần lượt thành "1", "2", "A", "B".

### Bước 3: Tạo/Hiệu chỉnh Cao độ (Levels)
Kiểm tra hệ thống Level trong file dự án:
* Nếu chưa có Level theo yêu cầu: Tạo mới các Level tương ứng.
* Nếu đã có sẵn: Điều chỉnh cao độ (`Elevation`) về đúng thiết kế:
    * `Level 1` tại cao độ `0`.
    * `Level 2` tại cao độ `3600mm`.
    * `Level Mái` tại cao độ `6900mm` ($3600 + 3300$).

### Bước 4: Dựng hệ Cột kết cấu (Columns)
* Xác định 4 tọa độ giao điểm từ hệ Grid vừa tạo ở Bước 2: $(1,A), (1,B), (2,A), (2,B)$.
* Dựng 4 cây cột kết cấu (`StructuralColumns`) tại 4 giao điểm này.
* *Ràng buộc parameters:* `Base Level` = `Level 1`, `Top Level` = `Level 2`. `Base Offset` = `0`.

### Bước 5: Dựng hệ Dầm kết cấu (Beams)
Nối các đỉnh cột lại với nhau theo chu vi hình chữ nhật để tạo thành 4 thanh dầm kết cấu (`StructuralFraming`):
* Dầm 1: Từ giao điểm $(1,A)$ đến $(2,A)$
* Dầm 2: Từ giao điểm $(2,A)$ đến $(2,B)$
* Dầm 3: Từ giao điểm $(2,B)$ đến $(1,B)$
* Dầm 4: Từ giao điểm $(1,B)$ đến $(1,A)$
* *Ràng buộc:* Đặt dầm cố định tại `Level 2`.

### Bước 6: Dựng Tường bao kiến trúc (Walls)
* Tạo 4 bức tường chạy dọc theo biên dạng khép kín của 4 đường Grid.
* *Ràng buộc parameters:* `Base Level` = `Level 1`, `Top Level` = `Level 2`.
* *Vị trí định vị:* Chỉnh thuộc tính `Wall.LocationLine` về `WallLocationLine.CoreCenterline` để tim tường ăn khớp chính xác với đường Grid.

### Bước 7: Dựng Sàn tầng 2 (Floors)
* Tạo một vòng biên dạng khép kín (`CurveLoop` hoặc `CurveArray` tùy đời Revit) nối từ tọa độ 4 góc nhà.
* Dùng hàm `Floor.Create` để chèn tấm sàn kiến trúc/kết cấu nằm tại vị trí `Level 2`.

---

## 4. Gợi ý cấu trúc thư mục triển khai (Architecture)

```text
MyRevitHouseBuilder/
│
├── Commands/
│   └── BuildHouseCommand.cs   # Nhận lệnh PickPoint, quản lý Transaction (Start/Commit)
│
├── Models/
│   └── HouseConfig.cs         # Chứa cấu trúc lưu trữ biến (Width, Length, Heights...)
│
├── Services/
│   ├── GridService.cs         # Logic vẽ Grid, tính toán tọa độ giao điểm
│   ├── StructureService.cs    # Logic vẽ Cột, vẽ Dầm, vẽ Sàn
│   └── ArchitectureService.cs # Logic vẽ Tường bao
│
└── Utils/
    ├── RevitUnitUtils.cs      # Hàm static đổi mm <=> Feet
    └── FilterUtils.cs         # Hàm static lọc nhanh FamilySymbol, Level theo tên