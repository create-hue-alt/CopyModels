using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyModels.Plugin.Services
{
    /// <summary>
    ///  Обработчики событий Revit: подавление диалогов и ошибок
    /// </summary>
    internal class EventService : IDisposable
    {
        private readonly Application _app;
        private readonly Action<string> _lofInfo;
        private readonly Action<string> _logWarning;

        private bool _subscribed;

        public EventService(
            Application app,
            Action<string> logInfo = null,
            Action<string> logWarning = null)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _lofInfo = logInfo ?? (_ => { });
            _logWarning = logWarning ?? (_ => { });
        }

        // 
        // Подписка/ отписка
        // 

        public void Subscribe()
        {
            throw new NotImplementedException();
        }

        public void Unsubscribe() 
        { 
            throw new NotImplementedException(); 
        }

        public void Dispose() => Unsubscribe();

        // 
        // Обработчик ошибок
        // 

        private void OnFailureProcessing(object sender, FailuresProcessingEventArgs e)
        {
            throw new NotImplementedException();
        }

        // 
        // Обработчик диалогов
        // 

        private void OnDialogBoxShowing(object sender, DialogBoxShowingEventArgs e)
        {
            throw new NotImplementedException();
        }

    }
}
