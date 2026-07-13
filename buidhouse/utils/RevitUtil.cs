using System.Reflection;
using Autodesk.Revit.DB;

namespace Simpleform.buidhouse.utils;

public static class RevitUtil
{
     public static double convertToMeter(double numberConvertInMilliMeter)
     {
         return UnitUtils.ConvertToInternalUnits(numberConvertInMilliMeter, UnitTypeId.Millimeters);
     }

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

    //lấy đường dẫn của addin =>đầu ra của file addin .dll: Example: C:\Users\trung\source\repos\Simpleform\Simpleform\bin\Debug\Simpleform.dll
     public static string getAddinDirectory()
     {
         return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
     }

    //lấy đường dẫn của family => nối thêm relativePath vào đầu ra của getAddinDirectory(): Ex [Thư mục DLL] + \ + families\Columns\abc.rfa
     public static string resolveFamilyPath(string relativePath)
     {
         return Path.GetFullPath(Path.Combine(getAddinDirectory(), relativePath));
     }
}
