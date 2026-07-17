using Autodesk.Revit.DB;

namespace Simpleform.buidhouse.utils;

public static class ElementExtensions
{
    //Gán nhánh comment và comment cho một Element
    public static void SetMarkAndComment( this Element element, string mark = "", string comment = "")
    {
        if(element == null) return;

        //set mark (All_MODEL_MARK)
        if(!string.IsNullOrEmpty(mark))
        {
            Parameter markField = element.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
            if(markField != null && !markField.IsReadOnly) // có ô nhập liệu mark và ô đó không phải chỉ đọc
            {
                markField.Set(mark);
            }
        }

        //set comment (ALL_MODEL_INSTANCE_COMMENTS)
        if(!string.IsNullOrEmpty(comment))
        {
            Parameter commentField = element.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
            if(commentField != null && !commentField.IsReadOnly)
            {
                commentField.Set(comment);
            }
        }
    }
}