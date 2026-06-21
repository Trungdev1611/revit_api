using Autodesk.Revit.DB;

namespace Simpleform.buidhouse.utils;

//nơi chứa utils helpers
public static class RevitUtil
{
     public static double convertToMeter(double numberConvertInMilliMeter)
     {
         return UnitUtils.ConvertToInternalUnits(numberConvertInMilliMeter, UnitTypeId.Millimeters);
     }
}