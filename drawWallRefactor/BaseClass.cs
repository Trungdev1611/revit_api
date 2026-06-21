using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Simpleform.drawWallRefactor;

[Transaction(TransactionMode.Manual)]
public abstract class BaseClass :IExternalCommand //cannot use new keyword with abstract class
{
    protected UIApplication uiapp; //protected: just use in child class extends
    protected UIDocument uidoc;
    protected Document doc;
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        uiapp = commandData.Application;
        uidoc = uiapp.ActiveUIDocument;
        doc = uidoc.Document;
        try
        {
            return ExcuteInside(commandData, ref message, elements);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    protected abstract Result ExcuteInside(ExternalCommandData commandData, ref string message, ElementSet elements);
}