using System.Reflection;
using Autodesk.Revit.UI;

namespace Simpleform.buidhouse;

public class App : IExternalApplication
{
  public Result OnStartup(UIControlledApplication application)
  {
    // 1. Tạo một Tab mới trên thanh Ribbon của Revit
    string tabName = "Build House Tool";
    application.CreateRibbonTab(tabName);

    // 2. Tạo một Panel bên trong Tab
    RibbonPanel ribbonPanel = application.CreateRibbonPanel(tabName, "Automation");
    // 3. Lấy đường dẫn thực tế của file Simpleform.dll khi Revit load lên
    string assemblyPath = Assembly.GetExecutingAssembly().Location;

    // 4. Tạo định nghĩa Button và gán nó với Lệnh Command cũ của bạn
    PushButtonData buttonData = new PushButtonData(
        "btnBuildHouse",
        "Build House\nAutomation", // Chữ hiển thị trên nút bấm
        assemblyPath,
        "Simpleform.buidhouse.commands.BuildHouseCommand" // Khớp chính xác với FullClassName cũ của bạn
    );

    // 5. Thêm nút bấm vào thanh công cụ
    PushButton pushButton = ribbonPanel.AddItem(buttonData) as PushButton;
    return Result.Succeeded;
  }
  public Result OnShutdown(UIControlledApplication app)
  {
    return Result.Succeeded;
  }

}