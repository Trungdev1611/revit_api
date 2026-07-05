using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Simpleform.buidhouse.constant;
using Simpleform.buidhouse.models;
using Simpleform.buidhouse.utils;
using Simpleform.drawWallRefactor;

namespace Simpleform.buidhouse.services;
public class ColumnService
{

  public FamilyInstance CreateColumn(Document doc, ColumnConfig config, XYZ location)
  {
    FamilySymbol symbol = GetOrCreateFamilySymbol(doc, config);
    if (symbol == null) {
      return null;
    }
          FamilyInstance column = doc.Create.NewFamilyInstance(
            location, 
            symbol, 
            doc.GetElement(config.BaseLevelId) as Level, 
            Autodesk.Revit.DB.Structure.StructuralType.Column
        );

           if (column != null)
        {
            column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).Set(config.TopLevelId); //đỉnh cột với tầng nào
            column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).Set(config.BaseOffset / 304.8);
            column.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM).Set(config.TopOffset / 304.8);
        }
        return column;
  }

  public FamilySymbol GetOrCreateFamilySymbol(Document _doc, ColumnConfig columnConfig)
  {
    // ĐỊNH NGHĨA CÁC BIẾN CÒN THIẾU TỪ TRONG CONFIG (Sửa lỗi chưa khai báo biến)
    string symbolName = columnConfig.SymbolName; // (Giả định bạn đổi thuộc tính Symbol từ kiểu FamilySymbol sang string như thảo luận)
    string colCategory = columnConfig.TypeColumn.ToString().ToLower();

    // khai báo điều kiện tìm kiếm cả familyname và symbolname
    Func<Family, bool> isExistFamilyName = (f) => f.Name == columnConfig.FamilyName;

    // SUA LỖI: So sánh f.Name (string) với tên của Symbol (string) chứ không phải đối tượng FamilySymbol
    Func<FamilySymbol, bool> isExistSymbolName = (f) => f.Name == symbolName;

    // B1: tìm xem trong dự án đã có type column này chưa
    // SỬA LỖI LỒNG DELEGATE: Truyền biến f thẳng vào 2 hàm Func đã khai báo phía trên
    FamilySymbol familySymbol = _doc.GetFirstItemOrCondition<FamilySymbol>(
      f => isExistFamilyName(f.Family) && isExistSymbolName(f)
    );

    // nếu có rồi thì trả về, không thì tạo mới
    if (familySymbol != null)
    {
      return familySymbol;
    }

    // B2: kiểm tra xem nếu chưa có thì family đã được load chưa
    Family structuralFamily = _doc.GetFirstItemOrCondition<Family>(
      f => isExistFamilyName(f)
    );

    if (structuralFamily == null && Constant.ColumnTypes.TryGetValue(colCategory, out string familyPath))
    {
      if (File.Exists(familyPath))
      {
        // load family vào dự án - đầu ra out gán vào structuralFamily
        bool isLoadSuccess = _doc.LoadFamily(familyPath, out structuralFamily);
        if (!isLoadSuccess || structuralFamily == null)
        {
          TaskDialog.Show("Error", "Đường dẫn family không đúng hoặc không thể load được family");
          return null;
        }
      }
      else
      {
        TaskDialog.Show("Error", "Đường dẫn family không tồn tại trong máy tính");
        return null; // THÊM return null ĐỂ NGẮT HÀM NẾU LỖI FILE
      }
    }

    // BƯỚC 3: Tại thời điểm này, Family chắc chắn đã nằm trong dự án. 
    // Chúng ta sẽ lấy 1 Type bất kỳ hiện tại của Family này để nhân bản (Duplicate) thành Type (ví dụ 220x220)

    // 1. Lấy danh sách ID của tất cả các Type (Symbol) nằm bên trong Family này
    var symbolIds = structuralFamily?.GetFamilySymbolIds();

    // SỬA LỖI: Rút gọn điều kiện check null danh sách ID
    if (symbolIds == null || symbolIds.Count == 0)
    {
      TaskDialog.Show("Error", "Family này không chứa bất kỳ Type nào để nhân bản.");
      return null;
    }

    // 2. Lấy ra ID của Type đầu tiên làm "mẫu"
    ElementId firstSymbolId = symbolIds.First();
    FamilySymbol defaultSymbol = _doc.GetElement(firstSymbolId) as FamilySymbol;

    // duplicate
    FamilySymbol newSymbol = defaultSymbol.Duplicate(symbolName) as FamilySymbol;

    // Kích hoạt Type mới này để Revit sẵn sàng mang đi vẽ (Bắt buộc trong Revit API)
    if (newSymbol != null && !newSymbol.IsActive)
    {
      newSymbol.Activate();
    }

    // THÊM DÒNG NÀY: Hàm bắt buộc phải có giá trị trả về cuối cùng
    return newSymbol;
  }



}