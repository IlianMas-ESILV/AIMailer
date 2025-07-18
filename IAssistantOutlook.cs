/******* ---------------------------------------------------------------------
 * *****    IAssistant      Intelligence Artificial-powered Office Assistant
 * ***** ---------------------------------------------------------------------
 * *****   
 * *****    IAssistantOutlook.cs   Main source file - Editor windows
 * *****
 * ***** -- Author -----------------------------------------------------------
 * *****
 * ***** -- (c) Ilian Mas (ESILV A1) / June 2025
 * *****
 * ***** -- Major Changes ----------------------------------------------------
 * *****    10/07/25 - Ilian Mas - Initial version
 * ***** - -------------------------------------------------------------------
 ******/



using System;
using System.Runtime.InteropServices;               // Integration Outlook
using Microsoft.Office.Interop.Outlook;
//using Outlook = Microsoft.Office.Interop.Outlook;

namespace IAssistant
{
    internal class IAssistantOutlook
    {
        ///  Variables globales propres à ce package 
        private const string iAssistantOutlookAppName = "Outlook.Application";

        /// <summary>
        /// Envoi d'email via Outlook
        /// </summary>
        public static void OpenEmail(string body, string subject = "")
        {
            try
            {
                // Récupère une instance d'Outlook en cours (nécessaire qu'il soit lancé)
                Application outlookApp = (Application)Marshal.GetActiveObject(iAssistantOutlookAppName);

                // Crée un nouvel email
                MailItem mail = (MailItem)outlookApp.CreateItem(OlItemType.olMailItem);

                // Préremplir les champs
                //mail.To = "exemple@edu.devinci.fr";
                //mail.CC = "copie@devinci.fr";
                mail.Subject = subject;
                mail.Body = body;

                // Ouvre l'email (l'utilisateur pourra le modifier avant d’envoyer)
                mail.Display(true);  // true = modal
            }
            catch (COMException)
            {
                // Error Outlook not running
                IAssistant.ErrorShow("ERROR_EDITOR_OUTLOOKNOTRUNNING");
                return;
            }
            catch (SystemException ex)
            {
                // Error while calling Outlook 
                IAssistant.ErrorShow("ERROR_EDITOR_OUTLOOKSAVEDRAFT", ex.Message);
                return;
            }
        }


        ///
        /// Fenetre d'ouverture de Rdv Outlook
        /// 
        public static void OpenRdv(string body, string subject = "", string location = "")
        {
            try
            {
                // Récupère l'instance Outlook en cours
                Application outlookApp = (Application)Marshal.GetActiveObject(iAssistantOutlookAppName);

                // Crée un nouveau rendez-vous
                AppointmentItem rdv = (AppointmentItem)
                    outlookApp.CreateItem(OlItemType.olAppointmentItem);

                // Remplit les informations de base
                rdv.Subject = subject;
                rdv.Start = DateTime.Now.AddHours(1);      // Heure de début
                rdv.End = DateTime.Now.AddHours(2);        // Heure de fin
                rdv.Location = location;
                rdv.Body = body;
                rdv.ReminderMinutesBeforeStart = 15;
                rdv.BusyStatus = OlBusyStatus.olBusy;

                // Affiche le rendez-vous sans l'enregistrer automatiquement
                rdv.Display(true); // true = modal, false = non-modal
            }
            catch (COMException)
            {
                // Error Outlook not running
                IAssistant.ErrorShow("ERROR_EDITOR_OUTLOOKNOTRUNNING");
                return;
            }
            catch (System.Exception ex)
            {
                // Error while calling Outlook 
                IAssistant.ErrorShow("ERROR_EDITOR_OUTLOOKSAVEDRAFT", ex.Message);
                return;
            }
        }
    }
}
