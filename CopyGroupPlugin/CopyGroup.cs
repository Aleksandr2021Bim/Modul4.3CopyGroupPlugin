using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyGroupPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CopyGroup : IExternalCommand
    {
        // код (для класса) команды что делать
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
               // выбираем документ или вид
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // создать отлылку к фильтру который указан в классе ниже 
                GroupPickFilter groupPickFilter = new GroupPickFilter();
               
                // начинаем общаться с программой ревит, а именно выбираем элемент или группу
                Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, "Выберите группу объектов");
                Element element = doc.GetElement(reference);
                Group group = element as Group;

                XYZ groupCenter = GetElementCenter(group);// выбраем ту комнату куда надо вставить группу
                Room room = GetRoomByPoint(doc, groupCenter); // выбираем ту группу где находиться она в комнате
                XYZ roomCenter = GetElementCenter(room); // находим центр этой комнаты
                XYZ offset = groupCenter - roomCenter; //  определяем смещение центра группы относительно центра комнаты

                
                // выбираем точку куда необходимо перенести элемент или группу
                XYZ point = uiDoc.Selection.PickPoint("Выберите точку");

                // задание 2 модуль 4 "в выбранной комнате для вставки объектов,  затем выбрать точку
                // после чего программа должна определить центр комнаты и по точке смещения "offset" и вставить
                // её в эту точку, при этом объект вставиться без смещения\\"
                Room userRoom = GetRoomByPoint(doc, point);
                XYZ userRoomCenter = GetElementCenter(userRoom);
                XYZ userGroupCenter = userRoomCenter + offset;

                // выполнение непосредственно операции что надо выполнить действие
                Transaction transaction = new Transaction(doc);
                transaction.Start("Копирование группы объектов");
                doc.Create.PlaceGroup(point, group.GroupType);
                transaction.Commit();
            }
            // чтобы не было ошибки при нажатии Esc
            catch(Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch(Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
            // завершение действия и возврат команды
            return Result.Succeeded;
        }

        // метод определения центра комнаты

        public XYZ GetElementCenter(Element element)
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);
            return (bounding.Max + bounding.Min) / 2;
        }

        // метод для определения комнаты

        public Room GetRoomByPoint(Document doc, XYZ point)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            foreach(Element e in collector)
            {
                Room room = e as Room;
                if (room!=null)
                {
                    if (room.IsPointInRoom(point))
                        return room;
                }
            }
            return null;
        }

    }

   

    // создание класса для фильтра среди элементов

    public class GroupPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)
                return true;
            else
                return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
