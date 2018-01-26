using ArchestrA.GRAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace AttributeWrangler
{
    public static class DatabasteFunctions
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static ArchestrAObject GetDerivationTree(string node, string galaxy, string template = "$UserDefined")
        {
            try
            {
                Dictionary<int, ArchestrAObject> objects = new Dictionary<int, ArchestrAObject>();

                string connectionString = string.Format("Data Source={0};Initial Catalog={1}; Integrated Security=SSPI;", node, galaxy);
                int id = 0;
                Dictionary<string, EgObjectIsTemplateOrInstance> results = new Dictionary<string, EgObjectIsTemplateOrInstance>();
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var command = new SqlCommand("select gobject_id from gobject where tag_name like @p", conn))
                    {
                        command.Parameters.AddWithValue("p", template);
                        var result = command.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                        {
                            _log.Warn(string.Format("Could not find template {0} in galaxy", template));
                            return null;
                        }
                        id = (int)result;
                    }

                    using (var command = new SqlCommand("internal_get_derivation_tree", conn)
                    {
                        CommandType = CommandType.StoredProcedure
                    })
                    {
                        command.Parameters.AddWithValue("gObjectId", id);
                        var reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            _log.Debug(string.Format("Found descendant - Name:{0}, Parent:{1}, IsTemplate:{2}", reader["gobject_name"].ToString(), reader["derived_from_gobject_name"].ToString(), reader["istemplate"].ToString()));
                            var obj = new ArchestrAObject() { Name = (string)reader["gobject_name"], IsTemplate = (bool)reader["istemplate"], ObjectID = (int)reader["gobject_id"], ParentObjectID = (int)reader["derived_from_gobject_id"] };
                            objects.Add(obj.ObjectID, obj);

                        }
                    }
                }
                foreach (var kvp in objects)
                {
                    if (objects.ContainsKey(kvp.Value.ParentObjectID))
                    {
                        objects[kvp.Value.ParentObjectID].Children.Add(kvp.Value);
                        kvp.Value.Parent = objects[kvp.Value.ParentObjectID];
                    }
                }
                return objects[id];
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
                return null;
            }
        }

        public static List<ArchestrAObject> GetAreas(string node, string galaxy)
        {
            try
            {
                List<ArchestrAObject> objects = new List<ArchestrAObject>();

                string connectionString = string.Format("Data Source={0};Initial Catalog={1}; Integrated Security=SSPI;", node, galaxy);
                Dictionary<string, EgObjectIsTemplateOrInstance> results = new Dictionary<string, EgObjectIsTemplateOrInstance>();
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    
                    using (var command = new SqlCommand("internal_get_all_areas", conn)
                    {
                        CommandType = CommandType.StoredProcedure
                    })
                    {                     
                        var reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            _log.Debug(string.Format("Found area - Name:{0}", reader["tag_name"].ToString()));
                            var obj = new ArchestrAObject() { Name = (string)reader["tag_name"], ObjectID = (int)reader["gobject_id"], AreaID = (int)reader["area_gobject_id"] };
                            objects.Add(obj);
                        }
                    }
                }
                return objects;
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
                return null;
            }

        }

        public static List<ArchestrAObject> GetAllObjects(string node, string galaxy)
        {
            try
            {
                List<ArchestrAObject> objects = new List<ArchestrAObject>();

                string connectionString = string.Format("Data Source={0};Initial Catalog={1}; Integrated Security=SSPI;", node, galaxy);
                Dictionary<string, EgObjectIsTemplateOrInstance> results = new Dictionary<string, EgObjectIsTemplateOrInstance>();
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var command = new SqlCommand("select * from gobject where namespace_id=1", conn))
                    {
                        var reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            _log.Debug(string.Format("Found object - Name:{0}, IsTemplate:{1}", reader["tag_name"].ToString(), reader["is_template"].ToString()));
                            var obj = new ArchestrAObject() { Name = (string)reader["tag_name"], IsTemplate = (bool)reader["is_template"], ObjectID = (int)reader["gobject_id"], ParentObjectID = (int)reader["derived_from_gobject_id"], AreaID = (int)reader["area_gobject_id"] };
                            objects.Add(obj);
                        }
                    }
                }
                return objects;
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
                return null;
            }

        }

        public static List<ArchestrAObject> GetAllObjectsInArea(string node, string galaxy, int areaId)
        {
            try
            {
                List<ArchestrAObject> objects = new List<ArchestrAObject>();

                string connectionString = string.Format("Data Source={0};Initial Catalog={1}; Integrated Security=SSPI;", node, galaxy);
                Dictionary<string, EgObjectIsTemplateOrInstance> results = new Dictionary<string, EgObjectIsTemplateOrInstance>();
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    List<int> areas = new List<int>();
                    using (var command = new SqlCommand("internal_get_all_objects_contained_by_gobject", conn)
                    {
                        CommandType = CommandType.StoredProcedure
                    })
                    {
                        command.Parameters.AddWithValue("gobject_id", areaId);
                        var reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            _log.Debug(string.Format("Found child area - ID:{0}", reader["gobject_id"].ToString()));
                            areas.Add((int)reader["gobject_id"]);
                        }
                        reader.Close();
                    }
                    
                    foreach (int id in areas)
                    {
                        using (var command = new SqlCommand("internal_list_objects_for_area", conn)
                        {
                            CommandType = CommandType.StoredProcedure
                        })
                        {
                            command.Parameters.AddWithValue("varwhere", id);
                            var reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                _log.Debug(string.Format("Found object in area {0} - Name:{1}", id, reader["tag_name"].ToString()));
                                var obj = new ArchestrAObject() { Name = (string)reader["tag_name"], IsTemplate = false, ObjectID = (int)reader["gobject_id"], ParentObjectID = (int)reader["derived_from_id"], AreaID = (int)reader["area_id"] };
                                objects.Add(obj);
                            }
                            reader.Close();
                        }
                    }
                }
                return objects;
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
                return null;
            }
        }

        public static List<ArchestrAObject> GetAllObjectsDerivedFrom(string node, string galaxy, int templateId)
        {
            try
            {
                List<ArchestrAObject> objects = new List<ArchestrAObject>();

                string connectionString = string.Format("Data Source={0};Initial Catalog={1}; Integrated Security=SSPI;", node, galaxy);
                Dictionary<string, EgObjectIsTemplateOrInstance> results = new Dictionary<string, EgObjectIsTemplateOrInstance>();
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var command = new SqlCommand("internal_get_derivation_tree", conn)
                    {
                        CommandType = CommandType.StoredProcedure
                    })
                    {
                        command.Parameters.AddWithValue("gObjectId", templateId);
                        var reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            _log.Debug(string.Format("Found descendant - Name:{0}, Parent:{1}, IsTemplate:{2}", reader["gobject_name"].ToString(), reader["derived_from_gobject_name"].ToString(), reader["istemplate"].ToString()));
                            var obj = new ArchestrAObject() { Name = (string)reader["gobject_name"], IsTemplate = (bool)reader["istemplate"], ObjectID = (int)reader["gobject_id"], ParentObjectID = (int)reader["derived_from_gobject_id"] };
                            objects.Add(obj);

                        }
                    }
                }
                return objects;
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString());
                return null;
            }
        }

    }
}
