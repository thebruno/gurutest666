using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Mail;
using System.Diagnostics;

namespace GuruTest
{
    public static class MailManagement
    {
        static string testGuruEmail = "test.guru.fp@gmail.com";
        static string testGuruEmailLogin = "test.guru.fp";
        static string testGuruPassword = "test.guru";
        static string smtpServer = "smtp.gmail.com";
        static int port = 587;
        static string mailTemplate = "Witaj w TestGuru.\n\nAby dokończyć proces rejestracji, należy aktywować swoje konto, klikając w poniższy link w ciągu 24 godzin. W innym wypadku konto zostanie usunięte.\n\nLink aktywujący konto:\n\nhttp://localhost:49305/TestGuru/Core/AccountActivation.aspx?ActivationGUID={0}\n\nJeśli nie rejestrowałeś się w portalu TestGuru, to ktoś inny zrobił to podając ten adres e-mail. W takim wypadku możesz po prostu zignorować tą wiadomość lub skorzystać z niżej przedstawionej opcji.\n\nJeśli notorycznie otrzymujesz od TestGuru wbrew swojej woli wiadomości z linkiem aktywacyjnym, a nie zamierzasz się nigdy rejestrować na tym portalu, możesz zablokować na zawsze możliwość podawania tego adresu e-mail podczas rejestracji na TestGuru, klikając w poniższy link.\n\nLink trwale blokujący ten adres e-mail:\n\nhttp://localhost:49305/TestGuru/Core/AccountActivation.aspx?TerminationGUID={1}\n\nTestGuru.pl";
        static string newPasswordTemplate = "Witaj.\n\nTwoje hasło na TestGuru.pl zostało zmienione.\nNowe hasło to {0}.\nHasło można zmienić po zalogowaniu się w portalu TestGuru.pl.";


        public static void Initialize()
        {

        }

        public static bool SendActivationEmail(TGMembershipUser user)
        {
            //ConfigReader Config = new ConfigReader();
            MailMessage message = new MailMessage();
            // Set the mail message fields.
            message.Subject = "Aktywuj swoje konto na TestGuru.pl";
            message.IsBodyHtml = false;
            message.From = new MailAddress(testGuruEmail);
            message.To.Add(new MailAddress(user.Email));
            message.Body = String.Format(mailTemplate, user.ActivationGUID.ToString(), user.TerminationGUID.ToString());

            SmtpClient smtpClient = new SmtpClient(smtpServer, port);
            smtpClient.EnableSsl = true;
            smtpClient.Credentials = new System.Net.NetworkCredential(testGuruEmailLogin, testGuruPassword);
            smtpClient.Send(message);
            return true;
        }

        public static bool SendNewPasswordEmail(Account user, string newPassword)
        {
            MailMessage message = new MailMessage();
            // Set the mail message fields.
            message.Subject = "Zmiana hasła na TestGuru.pl";
            message.IsBodyHtml = false;
            message.From = new MailAddress(testGuruEmail);
            message.To.Add(new MailAddress(user.Email));
            message.Body = String.Format(newPasswordTemplate, newPassword);

            SmtpClient smtpClient = new SmtpClient(smtpServer, port);
            smtpClient.EnableSsl = true;
            smtpClient.Credentials = new System.Net.NetworkCredential(testGuruEmailLogin, testGuruPassword);
            smtpClient.Send(message);
            return true;
        }
    }
}
