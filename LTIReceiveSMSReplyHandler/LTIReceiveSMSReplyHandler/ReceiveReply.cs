using System;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Communication.Sms;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Azure.Core;
using PhoneNumbers;
using System.Net.Mail;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Reflection.PortableExecutable;

namespace FunctionApp1
{
    public class SMSReceiveReply
    {
        private readonly ILogger<SMSReceiveReply> _logger;
        //private readonly DatabaseServices _dbServices;
        private readonly SmsClient _smsClient;
        private readonly string _fromNumber;

        public SMSReceiveReply(ILogger<SMSReceiveReply> logger, IConfiguration configuration)
        {
            _logger = logger;
            // Read the connection string and sender number from appsettings.json
            string connectionString = configuration["AzureCommunicationServices.ConnectionString"];
            _fromNumber = configuration["AzureCommunicationServices.FromNumber"];


            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(_fromNumber))
            {
                throw new InvalidOperationException("Azure Communication Services connection string or sender number is not set in configuration.");
            }

            _smsClient = new SmsClient(connectionString);
        }

        [Function(nameof(SMSReceiveReply))]
        public async Task Run([EventGridTrigger] CloudEvent cloudEvent)
        {
            _logger.LogInformation("Event type: {type}, Event subject: {subject}", cloudEvent.Type, cloudEvent.Subject);

            if (cloudEvent.Type == "Microsoft.Communication.SMSReceived")
            {
                var smsEvent = JsonConvert.DeserializeObject<SmsReceivedEventData>(cloudEvent.Data.ToString());

                if (smsEvent != null)
                {
                    string senderPhoneNumber = smsEvent.From;
                    string message = smsEvent.Message?.Trim().ToUpper();
                    string dbPhoneNumber = senderPhoneNumber.Substring(2);

                    //await _dbServices.LogNotificationEvent("N/A", senderPhoneNumber, "sms", "REPLY", "reply", "[AF/Reply] Received SMS reply", null, null);
                    _logger.LogInformation($"Received SMS from {senderPhoneNumber}: {message}");
                    

                    //Validating Phone Number:
                    if (!IsValidPhoneNumber(dbPhoneNumber))
                    {
                        //await _dbServices.LogNotificationEvent("N/A", senderPhoneNumber, "sms", "ERROR", "reply", "[AF/Reply] Invalid phone number format", null, null);
                        _logger.LogError("Invalid phone number format");
                    }
                    else
                    {
                        //await _dbServices.LogNotificationEvent("N/A", senderPhoneNumber, "sms", "REPLY", "reply", "[AF/Reply] Phone number format validated.", null, null);
                        _logger.LogInformation($"Phone number format validated.");
                    }
                    //Check if User exists:
                    //
                    /*
                    string email = await ValidateUserThroughPhone(dbPhoneNumber);

                    //Validate Email
                    
                    if (string.IsNullOrWhiteSpace(email))
                    {
                        //await _dbServices.LogNotificationEvent(email, senderPhoneNumber, "sms", "ERROR", "reply", "[AF/Reply] No user found for this phone number.", null, null);
                        _logger.LogError("No user found for this phone number.");
                    }

                    // Validate email format before proceeding
                    if (!IsValidEmail(email))
                    {
                        //await _dbServices.LogNotificationEvent(email, senderPhoneNumber, "sms", "ERROR", "reply", "[AF/Reply] Invalid email format associated with this phone number!", null, null);
                        _logger.LogError("Invalid email format associated with this phone number!");
                    }

                    //check if user is opted in
                    bool userOptedIn = await CheckOptInStatus(email);
                    if (!userOptedIn)
                    {
                        //await _dbServices.LogNotificationEvent(email, senderPhoneNumber, "sms", "ERROR", "reply", "[AF/Reply] User already opted out.", null, null);
                        _logger.LogError("User already opted out.");
                    }
                    */
                    //Read message
                    if (message == "OPT-OUT")
                    {
                        // Opt out user from database table
                        /*
                        bool userOptedOut = await OptUserOutFromSMS(email, dbPhoneNumber);

                        if (!userOptedOut)
                        {
                            //await _dbServices.LogNotificationEvent(email, senderPhoneNumber, "sms", "ERROR", "reply", "[AF/Reply] Failed to opt user out.", null, null);
                            _logger.LogError("Failed to opt user out.");
                        }
                        */
                        string optOutMessage = "You have been successfully opted-out of notifications!";
                        if (SendSMS(senderPhoneNumber, optOutMessage))
                        {
                            //await _dbServices.LogNotificationEvent(email, senderPhoneNumber, "sms", "REPLY", "reply", "[AF/Reply/Opt-Out] SMS Message successfully sent!", null, null);
                            _logger.LogInformation("Opt-out confirmation SMS sent successfully.");
                        }
                        else
                        {
                            //await _dbServices.LogNotificationEvent(email, senderPhoneNumber, "sms", "ERROR", "reply", "[AF/Reply/Opt-Out] SMS sending failed.", null, null);
                            _logger.LogError("Failed to send opt-out confirmation SMS.");
                        }
                    }
                    else if (message == "HELP")
                    {
                        string helpMessage = "To opt-out of LTI Notifications, text OPT-OUT. Avoid texting STOP to stay connected. If you accidentally text STOP, follow the instructions to opt back in.";
                        if (SendSMS(senderPhoneNumber, helpMessage))
                        {
                            //await _dbServices.LogNotificationEvent(email, senderPhoneNumber, "sms", "REPLY", "reply", "[AF/Reply/HELP] SMS Message successfully sent!", null, null);
                            _logger.LogInformation("HELP message SMS sent successfully.");
                        }
                        else
                        {
                            //await _dbServices.LogNotificationEvent(email, senderPhoneNumber, "sms", "ERROR", "reply", "[AF/Reply/HELP] SMS sending failed.", null, null);
                            _logger.LogError("Failed to send HELP message SMS.");
                        }
                    }
                    else
                    {
                        //await _dbServices.LogNotificationEvent(email, senderPhoneNumber, "sms", "ERROR", "reply", "Invalid message received.", null, null);
                        _logger.LogError("Invalid message received.");
                    }
                }
                else
                {
                    _logger.LogWarning("The received event data could not be deserialized.");
                }
            }
            else
            {
                _logger.LogWarning($"Unhandled Event Type: {cloudEvent.Type}");
            }
        }

        private bool SendSMS(string recipient, string message)
        {
            try
            {
                var response = _smsClient.SendAsync(
                    from: _fromNumber,
                    to: recipient,
                    message: message
                ).Result;

                return response.Value.Successful;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending SMS: {ex.Message}");
                return false;
            }
        }

        // Helper function for validating phone numbers under Azure CS policy
        public static bool IsValidPhoneNumber(string phoneNumber, string region = "US")
        {
            var phoneNumberUtil = PhoneNumbers.PhoneNumberUtil.GetInstance();
            try
            {
                var parsedNumber = phoneNumberUtil.Parse(phoneNumber, region);
                return phoneNumberUtil.IsValidNumber(parsedNumber);
            }
            catch (NumberParseException)
            {
                return false;
            }
        }

        public class SmsReceivedEventData
        {
            public string From { get; set; }
            public string Message { get; set; }
        }
    }
}
