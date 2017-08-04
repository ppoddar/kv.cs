using System;
using NUnit.Framework;
using oracle.kv.client.data;
namespace oracle.kv.client.test {

    enum DataTemplate { IMAGE, ARRAY, RECORD };

    //static readonly int COUNT = 1000000;
    //static readonly int WARMING_INDEX = 10;


#pragma warning disable 0649

    public class SerDePerfTest : AbstractDatbaseTest {

        ISerializationSupport serde;

        //[Test]
        //[Category("LongRunning")]
        public void testJson() {
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine(string.Format("{0} \t {1} \t {2} \t {3}",
                                           "Template", "Count", "Bytes", "Time"));
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine(" \t \t \t(KB)\tms/row");
            Console.WriteLine("------------------------------------------------");

            int size = 10;
            int factor = 10;
            for (int i = 0; i < 3; i++) {
                Console.WriteLine("------------------------------------------------");
                timeImpls(size = size * factor);
            }
            Console.WriteLine("------------------------------------------------");

        }




        /**
         * Prints time taken to serialize row data using each template.
         */
        void timeImpls(int size) {

            foreach (DataTemplate t in Enum.GetValues(typeof(DataTemplate))) {
                int count = 100;
                var result = timeSerializeRow(count, size, t);
                long byteSize = result[1] / 1000;
                long time = result[0] / (100 * count);
                Console.WriteLine(string.Format("{0} \t {1} \t {2} \t {3}",
                                                t, count, byteSize, time));
            }
        }


        /**
         * measure time and bytes to serialize row 
         * 
         * @param size size of data
         * @param template to use
         * @param t reference time
         */
        long[] timeSerializeRow(int count, int size, DataTemplate template) {
            Console.WriteLine("timeSerializeRow() count=" + count
                + " template=" + template);
            long startTime = DateTime.Now.Ticks;
            long byteSize = 0;
            for (int i = 0; i < count; i++) {
                var serializedRow = serializeRow(size, template);
                Console.WriteLine("serializedRow:" + serializedRow);
                byteSize += (serializedRow == null ? 0 : serializedRow.Length);
            }
            long elapsedTicks = DateTime.Now.Ticks - startTime;
            Console.WriteLine("timeSerializeRow() time=" +
                elapsedTicks / 1000 + " bytes=" + byteSize / 1000 + "KB");
            return new long[] { elapsedTicks, byteSize };
        }


        string serializeRow(int size, DataTemplate template) {
            IKVStore store = GetDriver().GetStore();
            IRow r = store.CreateRow("");
            r["entryId"] = size;
            Console.Write("timeSerializeRow() size=" + size
                + " template=" + template + " ...");
            switch (template) {
                case DataTemplate.IMAGE:
                    byte[] image = new byte[size];
                    r["image"] = image;
                    break;

                case DataTemplate.ARRAY:
                    int[] a = new int[size];
                    for (int i = 0; i < size; i++) {
                        a[i] = i;
                    }
                    r["array"] = a;

                    break;

                case DataTemplate.RECORD:
                    var record = store.CreateRow("");
                    for (int i = 0; i < size; i++) {
                        record["f" + i] = i;
                    }
                    r["record"] = record;
                    break;
            }
            Console.WriteLine("Done.");
            return r.ToJSONString();
        }

        //[Test]
        //[Category("Performance")]
        public void testPerf() {
            string jsonString = @"{""name"":""hello world"", ""age"":42, ""purpose"":""measure""}";
            int size = jsonString.Length;

            int N = 10;
            long start = DateTime.Now.Ticks;
            for (int i = 0; i < N; i++) {
                serde.Deserialize(jsonString);
            }
            // double elapsed = (DateTime.Now.Ticks - start) / (1.0E+5);
            // long byteLength = N * size;
            // double kbps = byteLength / elapsed;
            // Console.WriteLine("no of samples:" + N + " time taken:"
            // + elapsed + " ms rate:" + kbps + "kb/s");
        }
    }
}



