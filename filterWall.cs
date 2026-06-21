using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Simpleform;

[Transaction(TransactionMode.Manual)]
public class FilterWall: IExternalCommand
{
    private Double feetToMeter = 3.28084;

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uiDocument = commandData.Application.ActiveUIDocument;
        Document doc = uiDocument.Document;

        try
        {
            List<Element> listOfWall = new FilteredElementCollector(doc)
                .OfClass(typeof(Wall))
                .WhereElementIsNotElementType()
                .ToList();
            int count = 0;

            List<string> listOfWallLeengthInMeter = listOfWall.Select(x =>
            {
                Parameter lenParam = x.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
                if (lenParam == null) return "0";
                double lengthInMeter = lenParam.AsDouble() / feetToMeter;
                if (lengthInMeter > 10) count++;
                return lengthInMeter.ToString("0.##");
            }).ToList();
            ;
            string stringResult = string.Join("m, ", listOfWallLeengthInMeter);
            TaskDialog.Show("Thong bao",
                $"Chieu dai cua cac buc tuong hệ meter là: {stringResult}" +
                $" và count>10m là: {count}");
            return Result.Succeeded;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
