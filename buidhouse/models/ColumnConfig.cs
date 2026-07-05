
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
namespace Simpleform.buidhouse.models;

public enum ColumnShape {
    square,
    rectangular,
    circular
}
public record ColumnConfig( //record sẽ là readonly, và khi khởi tạo 2 record giống nhau thì so sánh == được
    string SymbolName,       // Tên Type mong muốn. Ví dụ: "220x220"
    ElementId BaseLevelId,   // Dùng Id để so sánh == chính xác
    ElementId TopLevelId,    //đỉnh cột với tầng nào
    double BaseOffset = 0.0, //khoảng cách từ tầng dưới đến đáy chân cột
    double TopOffset = 0.0, //khoảng cách từ đỉnh cột đến tầng trên
    ColumnShape TypeColumn = ColumnShape.square, //hình dạng cột
    string FamilyName = "Concrete-Square-Column" //cột kết cấu bê tông
  );

