using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using System;
using System.Collections.Generic;

namespace CopyModels.Plugin.Services
{
    /// <summary>
    ///  Обработчики событий Revit: подавление диалогов и ошибок
    /// </summary>
    internal class EventService : IDisposable
    {
        private readonly Application _appFailure;
        private readonly UIApplication _appDialog;
        private readonly Action<string> _logInfo;
        private readonly Action<string> _logWarning;
        private readonly Action<string> _logDebug;

        private bool _subscribed;

        public EventService(
            Application appFailure,
            UIApplication appDialog,
            Action<string> logInfo = null,
            Action<string> logWarning = null,
            Action<string> logDebug = null)
        {
            _appFailure = appFailure ?? throw new ArgumentNullException(nameof(appFailure));
            _appDialog = appDialog ?? throw new ArgumentNullException(nameof(appDialog));
            _logInfo = logInfo ?? (_ => { });
            _logWarning = logWarning ?? (_ => { });
            _logDebug = logDebug ?? (_ => { });
        }

        // 
        // Подписка/ отпискаS
        // 

        public void Subscribe()
        {
            if (_subscribed) return;
            _appFailure.FailuresProcessing += OnFailureProcessing;
            _appDialog.DialogBoxShowing += OnDialogBoxShowing;
            _subscribed = true;
        }

        public void Unsubscribe()
        {
            _appFailure.FailuresProcessing -= OnFailureProcessing;
            _appDialog.DialogBoxShowing -= OnDialogBoxShowing;
            _subscribed = false;
        }

        public void Dispose() => Unsubscribe();

        // 
        // Обработчик ошибок
        // 

        private void OnFailureProcessing(object sender, FailuresProcessingEventArgs e)
        {
            var accessor = e.GetFailuresAccessor();
            var messages = accessor.GetFailureMessages();

            if (messages.Count == 0)
            {
                e.SetProcessingResult(FailureProcessingResult.Continue);
                return;
            }

            accessor.DeleteAllWarnings();
            messages = accessor.GetFailureMessages();

            if (messages.Count == 0)
            {
                e.SetProcessingResult(FailureProcessingResult.ProceedWithCommit);
                return;
            }

            var unresolved = new List<string>();

            foreach (FailureMessageAccessor failure in messages)
            {
                var id =failure.GetFailureDefinitionId();
                
                if (id == BuiltInFailures.DimensionFailures.LinearConstraintNotParallel)
                {
                    accessor.ResolveFailure(failure);
                }
                else if (id == BuiltInFailures.DimensionFailures.DimensionReferencesInvalid)
                {
                    failure.SetCurrentResolutionType(FailureResolutionType.FixElements);
                    accessor.ResolveFailure(failure);
                }
                else if (id == BuiltInFailures.GeneralFailures.GenericNonFatalError)
                {
                    _logWarning($"Generic failure: {failure.GetDescriptionText()}");
                    accessor.ResolveFailure(failure);
                }
                else
                {
                    unresolved.Add($"{failure.GetDescriptionText()} [{id.Guid}]");
                }
            }

            accessor.DeleteAllWarnings();
            if (unresolved.Count > 0)
                _logWarning("Uncleared failures:\n" + string.Join("\n", unresolved));

            e.SetProcessingResult(FailureProcessingResult.ProceedWithCommit);
        }

        // 
        // Обработчик диалогов
        // 

        private void OnDialogBoxShowing(object sender, DialogBoxShowingEventArgs e)
        {
            _logInfo($"Dialog appeared: {e.DialogId}");

            switch (e.DialogId)
            {
                case "TaskDialog_Missing_Third_Party_Updater":
                case "TaskDialog_Missing_Third_Party_Updaters":
                    e.OverrideResult((int)TaskDialogResult.CommandLink1);
                    _logInfo($"{e.DialogId}: override CommandLink1");
                    break;

                case "TaskDialog_Unresolved_References":
                    e.OverrideResult((int)TaskDialogResult.CommandLink2);
                    _logInfo($"{e.DialogId}: override CommandLink2");
                    break;

                case "TaskDialog_Update_Resources":
                case "TaskDialog_Macro_Security_Alert":
                    e.OverrideResult((int)TaskDialogResult.CommandLink1);
                    _logInfo($"{e.DialogId}: override CommandLink1");
                    break;

                case "Dialog_Revit_DocWarnDialog":
                case "":
                    e.OverrideResult((int)TaskDialogResult.Close);
                    _logInfo($"{e.DialogId}: override Close");
                    break;

                default:
                    _logWarning($"Unknown dialog: \"{e.DialogId}\" — please report this.");
                    break;
            }
        }

    }
}
