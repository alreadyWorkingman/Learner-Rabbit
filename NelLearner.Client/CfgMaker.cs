using System;
using System.Linq;

namespace NetLearner.Client
{
    public class CfgMaker
    {
        private string cfgTemplatePath;
        private string dataForCfgPath;
        private string newCfgPath;
        private int sizeOfPicture;
        private const int newBatch = 64;
        private const int newSubdivisions = 16;
        private int newMaxBatches;
        private int newFiltersCount;
        private int classesCount;
        private int step1, step2;

        public CfgMaker(string cfgTemplatePath, string dataForCfgPath, string newCfgPath)
        {
            this.cfgTemplatePath = cfgTemplatePath; //планируется с именем файла
            this.dataForCfgPath = dataForCfgPath; //планируется с именем файла
            this.newCfgPath = newCfgPath; //планируется с именем файла
            this.sizeOfPicture = 416;

            classesCount = readClassCount(dataForCfgPath); //подготовка и вычисление значений 
            setBatchesAndStepsValues();
            setFiltersValue();

            System.IO.File.Copy(cfgTemplatePath, newCfgPath, true); // копирование кфг по шаблону
        }
        private int readClassCount(string path)
        {
           
            System.IO.StreamReader streamReader = new System.IO.StreamReader(path);
            do
            {
                var value = streamReader.ReadLine().Split('=');
                if (value[0] == "classes") return int.Parse(value[1].Trim());
            }
            while (!streamReader.EndOfStream);
            return 0;//? обработки ошибки нет
        }
        private void setBatchesAndStepsValues()
        {
            if (classesCount * 2000 < 6000)
                newMaxBatches = 6000;
            else
                newMaxBatches = classesCount * 2000;
            step1 = (int)((int)newMaxBatches * 0.8);
            step2 = (int)((int)newMaxBatches * 0.9);
        }
        private void setFiltersValue()
        {
            newFiltersCount = (classesCount + 5) * 3;
        }
        public void CreateNewCfg()
        {
            SetCfgValue(newCfgPath, "batch", newBatch.ToString()); //изменение парамтеров в новом кфг
            SetCfgValue(newCfgPath, "size", sizeOfPicture.ToString());
            SetCfgValue(newCfgPath, "subdivisions", newSubdivisions.ToString());
            SetCfgValue(newCfgPath, "max_batches", newMaxBatches.ToString());
            SetCfgValue(newCfgPath, "steps", step1.ToString() + "," + step2.ToString());
            SetFilterAndClassesInCfg(newCfgPath, "filters", newFiltersCount.ToString());
            SetFilterAndClassesInCfg(newCfgPath, "classes", classesCount.ToString());
        }
        private void SetFilterAndClassesInCfg(string cfgPath, string propertyName, string newValue) // Присваивание filters и classes
        {
            string buffer;
            string findingElement = "";
            using (System.IO.StreamReader reader = System.IO.File.OpenText(cfgPath))
            {
                buffer = reader.ReadToEnd();
            }
            if (propertyName == "filters")
            {
                findingElement = propertyName + '=' + "255";
            }
            else if (propertyName == "classes")
            {
                findingElement = propertyName;
            }
            int counter;
            int index = buffer.IndexOf('\n' + findingElement);
            while (index != -1)
            {
                counter = 0;
                index += propertyName.Length + 1;
                for (int i = index; i < buffer.Length - 1; i++)
                {
                    if (buffer[i] != '\r')
                    {
                        counter++;
                    }
                    else break;
                }
                buffer = buffer.Remove(index, counter);
                buffer = buffer.Insert(index, '=' + newValue);
                index = buffer.IndexOf('\n' + findingElement, index);
            }
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(cfgPath))
            {
                file.Write(buffer);
            }
        }

        public static void SetCfgValue(string cfgPath, string propertyName, string newValue)  // Присваивание параметров
        {
            string buffer;
            using (System.IO.StreamReader reader = System.IO.File.OpenText(cfgPath))
            {
                buffer = reader.ReadToEnd();
            }
            if (propertyName == "subdivisions" || propertyName == "batch" || propertyName == "max_batches" || propertyName == "steps")
            {
                int counter = 0;

                var index = buffer.IndexOf('\n' + propertyName);
                index += propertyName.Length + 1;
                for (int i = index; i < buffer.Length - 1; i++)
                {
                    if (buffer[i] != '\r')
                    {
                        counter++;
                    }
                    else break;
                }
                buffer = buffer.Remove(index, counter);
                buffer = buffer.Insert(index, '=' + newValue);
            }
            else if (propertyName == "size")
            {
                int counter = 0;
                var Index = buffer.IndexOf('\n' + "width");
                Index += 5 + 1; //количество букав в width
                for (int i = Index; i < buffer.Length - 1; i++)
                {
                    if (buffer[i] != '\r')
                    {
                        counter++;
                    }
                    else break;
                }
                buffer = buffer.Remove(Index, counter);
                buffer = buffer.Insert(Index, '=' + newValue);
                counter = 0;
                Index = buffer.IndexOf('\n' + "height");
                Index += 6 + 1; //количество букав в height 
                for (int i = Index; i < buffer.Length - 1; i++)
                {
                    if (buffer[i] != '\r')
                    {
                        counter++;
                    }
                    else break;
                }
                buffer = buffer.Remove(Index, counter);
                buffer = buffer.Insert(Index, '=' + newValue);
            }
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(cfgPath))
            {
                file.Write(buffer);
            }
        }
        public static int GetCfgValue(string cfgPath, string propertyName) // Возврат значений параметров
        {
            string fileText = System.IO.File.ReadAllText(cfgPath);
            var splitedText = fileText.Split(new Char[] { ' ', '.', ',', ':', '=', '\t', '\r', '\n' }).ToList();
            bool getNextValue = false;
            if (propertyName == "subdivisions")
            {
                foreach (var value in splitedText)
                {
                    if (value.Trim() == "")
                    {
                        continue;
                    }
                    if (getNextValue)
                    {
                        return int.Parse(value);
                    }
                    if (value == propertyName)
                    {
                        getNextValue = true;
                    }
                }
            }
            else if (propertyName == "size")
            {
                foreach (var value in splitedText)
                {
                    if (value.Trim() == "")
                    {
                        continue;
                    }
                    if (getNextValue)
                    {
                        return int.Parse(value);
                    }
                    if (value == "width")
                    {
                        getNextValue = true;
                    }
                }
            }
            return -1;
        }

    }
}
