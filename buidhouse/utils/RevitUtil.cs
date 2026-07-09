using System.Reflection;
using Autodesk.Revit.DB;

namespace Simpleform.buidhouse.utils;

/// <summary>Helper dùng chung: đơn vị, geometry, đường dẫn add-in.</summary>
public static class RevitUtil
{
    /// <summary>Đổi mm → feet (internal units của Revit API).</summary>
    public static double convertToMeter(double numberConvertInMilliMeter)
    {
        return UnitUtils.ConvertToInternalUnits(numberConvertInMilliMeter, UnitTypeId.Millimeters);
    }

    /// <summary>Tạo CurveLoop hình chữ nhật khép kín (4 cạnh nối góc) — dùng cho Floor.Create.</summary>
    public static CurveLoop createRectangleLoop(double left, double right, double bottom, double top, double z)
    {
        XYZ bottomLeft = new XYZ(left, bottom, z);
        XYZ bottomRight = new XYZ(right, bottom, z);
        XYZ topRight = new XYZ(right, top, z);
        XYZ topLeft = new XYZ(left, top, z);

        CurveLoop loop = new CurveLoop();
        loop.Append(Line.CreateBound(bottomLeft, bottomRight));
        loop.Append(Line.CreateBound(bottomRight, topRight));
        loop.Append(Line.CreateBound(topRight, topLeft));
        loop.Append(Line.CreateBound(topLeft, bottomLeft));
        return loop;
    }

    /// <summary>Thư mục chứa Simpleform.dll (buildhouse_Files sau khi build).</summary>
    public static string getAddinDirectory()
    {
        return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    }

    /// <summary>Ghép path tương đối (families/Columns/xxx.rfa) với thư mục DLL.</summary>
    public static string resolveFamilyPath(string relativePath)
    {
        return Path.GetFullPath(Path.Combine(getAddinDirectory(), relativePath));
    }
}
