using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace Simpleform.drawWallOnLine;

public class SelectionDraw: ISelectionFilter
{
    private readonly UIDocument _uidoc;

    public SelectionDraw(UIDocument uidoc)
    {
        this._uidoc = uidoc;
    }
    
    //tìm kiếm loại đường
    public bool AllowElement(Element elem)
    {
        return elem is ModelCurve || elem is DetailCurve;
    }

    //tìm kiếm hình học
    public bool AllowReference(Reference reference, XYZ position)
    {
        Element elm = _uidoc.Document.GetElement(reference.ElementId);
        Curve curve = null;
        if (elm is ModelCurve modelCurve)
        {
            curve = modelCurve.GeometryCurve;
        }
        else if (elm is DetailCurve detailCurve)
        {
            curve = detailCurve.GeometryCurve;
        }

        if (curve is Line || curve is Arc)
        {
            return true;
        }

        return false;
    }
}