using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

namespace DeleteLayer
{
    public class EraseLayer
    {
        [CommandMethod("ELL")]
        public void Program()
        {
            Database db = Application.DocumentManager.MdiActiveDocument.Database;

            List<string> info = LayersToList(db);

            foreach (var item in info)
            {
                LayerDelete(item);
            }
        }

        public List<string> LayersToList(Database db)
        {
            List<string> lstlay = new List<string>();

            LayerTableRecord layer;
            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                Regex regex = new Regex("L[1-5]", RegexOptions.None);

                LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                foreach (ObjectId layerId in lt)
                {
                    layer = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;

                    if (regex.IsMatch(layer.Name) &&
                        layer.Color == Color.FromColorIndex(ColorMethod.ByColor, 5) ||
                        layer.Name == "COTAS" ||
                        layer.Name == "L.D." )
                    {
                        lstlay.Add(layer.Name);
                    }
                }

            }
            return lstlay;
        }

        private void LayerDelete(string layerName)
        {
            Database db = Application.DocumentManager.MdiActiveDocument.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                db.Clayer = layerTable["0"];

                if (layerTable.Has(layerName))
                {
                    try
                    {
                        var layerId = layerTable[layerName];

                        var layer = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForWrite);

                        layer.IsLocked = false;

                        var blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                        foreach (var btrId in blockTable)
                        {
                            var block = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                            foreach (var entId in block)
                            {
                                var ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);
                                if (ent.Layer == layerName)
                                {
                                    ent.UpgradeOpen();
                                    ent.Erase();
                                }
                            }
                        }

                        layer.Erase();
                        tr.Commit();
                    }
                    catch (System.Exception)
                    {
                        // Não faz nada...
                    }
                }
            }
        }
    }
}
