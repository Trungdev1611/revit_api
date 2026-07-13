using System.Reflection;
using Autodesk.Revit.DB;
using Simpleform.buidhouse.utils;
using Simpleform.drawWallRefactor;

namespace BuildHouse.Utils;

public static class FamilyUtilClass
{



    //Try load family and trả ra familyloaded if load success
    public static bool TryLoadFamily(Document doc, string absolutePath, out Family? familyloaded)
    {

        return doc.LoadFamily(absolutePath, out familyloaded) && familyloaded != null;
    }

    public static bool CheckFilePathExist(string fullPath) {
        if(File.Exists(fullPath)) return true;
        return false;
    }

    //Get a family which was existed in the project or load new
    public static Family GetFamilyIfExistedOrloadNew(Document _doc, string familyName, string relativePath) {

        //check xem family đã tồn tại với familyName hiện tại chưa, nếu có trả về luôn
        Family familyloaded = _doc.GetFirstItemOrCondition<Family>(item => item.Name == familyName);
        if(familyloaded != null) return familyloaded;

        //nếu không có load family mới
        //get fullpath - nơi cần load family
        string fullPathToLoadFamily = RevitUtil.resolveFamilyPath(relativePath);

        bool isExistPath = CheckFilePathExist(fullPathToLoadFamily);
        if(isExistPath) {
            TryLoadFamily(_doc, fullPathToLoadFamily, out familyloaded);
        }
        return familyloaded;


    }

}
