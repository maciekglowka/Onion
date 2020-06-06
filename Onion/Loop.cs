using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Autodesk.DesignScript.Runtime;

//using System.IO.MemoryMappedFiles;
namespace Onion
{
    public static class Loop
    {
        /*
        static string mapFileName = "onion_loop_mapfile";
        [MultiReturn(new[] { "data" })]
        public static Dictionary<string, object> LoopStart(string bufferPath, object trigger, object initData)
        {
 
            object data = null;
            try
            {
                using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting(mapFileName))
                {
                    MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor();
                    BinaryFormatter bf = new BinaryFormatter();
                    using (var ms = new MemoryStream())
                    {
                        byte[] buffer = new byte[accessor.Capacity];
                        accessor.ReadArray<byte>(0, buffer, 0, buffer.Length);
                        ms.Write(buffer, 0, buffer.Length);
                        ms.Seek(0, SeekOrigin.Begin);
                        data = bf.Deserialize(ms);
                    }
                }
            }
            catch
            {

            }
            if (data == null)
            {
                data = initData;
            }
            return new Dictionary<string, object>
            {
                {"data", data }
            };
        }

        [MultiReturn(new[] { "data" })]
        public static Dictionary<string, object> LoopEnd(object data)
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(mapFileName, 1024))
            {
                MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor();
                accessor.Flush();
                BinaryFormatter bf = new BinaryFormatter();
                using (var ms = new MemoryStream())
                {
                    bf.Serialize(ms, data);
                    byte[] buffer = ms.ToArray();
                    accessor.WriteArray<byte>(0, buffer, 0, buffer.Length);
                }
            }
            return new Dictionary<string, object>
            {
                {"data", data }
            };
        }*/
        
        public static object LoopStart(string bufferPath, object trigger, [ArbitraryDimensionArrayImport] object initData)
        {
            IFormatter formatter = new BinaryFormatter();
            object data = null;
            try
            {
                Stream stream = new FileStream(bufferPath, FileMode.Open, FileAccess.Read);
                data = formatter.Deserialize(stream);
                stream.Close();
            }
            catch
            {

            }
            if (data == null)
            {
                data = initData;
            }
            return data;
        }

        public static object LoopEnd(string bufferPath, [ArbitraryDimensionArrayImport] object data)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(bufferPath, FileMode.Create, FileAccess.Write);

            formatter.Serialize(stream, data);
            stream.Close();

            return data;
        }
    }
}
