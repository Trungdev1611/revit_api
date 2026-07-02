using Autodesk.Revit.DB;

namespace Simpleform.buidhouse.utils;

//nơi chứa utils helpers
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
}
