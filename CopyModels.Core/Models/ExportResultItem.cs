using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyModels.Core.Models
{
    /// <summary>
    /// Результат обработки одной модели (успех или ошибка) - для окна итогов.
    /// </summary>
    public class ExportResultItem
    {
        public string ModelName { get; }
        
        public bool Success { get; }

        ///<summary>Целевой путь при успехи, исходный путь при ошибке</summary>
        public string Path { get; }

        ///<summary>Причинв ошибки. Null при успехи</summary>
        public string ErrorMessage { get; }

        public ExportResultItem(
            string modelName,
            bool success,
            string path,
            string errorMessage = null)
        {
            ModelName = modelName; 
            Success = success;
            Path = path;
            ErrorMessage = errorMessage;
        }
    }
}
