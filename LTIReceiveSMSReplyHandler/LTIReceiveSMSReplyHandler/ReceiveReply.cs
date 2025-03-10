using System;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Communication.Sms;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace FunctionApp1
{
    public class SMSReceiveReply
    {
        private readonly ILogger<SMSReceiveReply> _logger;
        private readonly SmsClient _smsClient;
        private readonly string _fromNumber;

        public SMSReceiveReply(ILogger<SMSReceiveReply> logger, IConfiguration configuration)
        {
            _logger = logger;

            // Read the connection string and sender number from appsettings.json
            string connectionString = configuration["AzureCommunicationServices:ConnectionString"];
            _fromNumber = configuration["AzureCommunicationServices:FromNumber"];

            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(_fromNumber))
            {
                throw new InvalidOperationException("Azure Communication Services connection string or sender number is not set in configuration.");
            }

            _smsClient = new SmsClient(connectionString);
        }

        [Function(nameof(SMSReceiveReply))]
        public void Run([EventGridTrigger] CloudEvent cloudEvent)
        {
            _logger.LogInformation("Event type: {type}, Event subject: {subject}", cloudEvent.Type, cloudEvent.Subject);

            if (cloudEvent.Type == "Microsoft.Communication.SMSReceived")
            {
                var smsEvent = JsonConvert.DeserializeObject<SmsReceivedEventData>(cloudEvent.Data.ToString());

                if (smsEvent != null)
                {
                    string senderPhoneNumber = smsEvent.From;
                    string message = smsEvent.Message?.Trim().ToUpper();

                    _logger.LogInformation($"Received SMS from {senderPhoneNumber}: {message}");

                    if (message == "OPT-OUT")
                    {
                        string optOutMessage = "You have been successfully opted-out of notifications!";
                        if (SendSMS(senderPhoneNumber, optOutMessage))
                        {
                            _logger.LogInformation("Opt-out confirmation SMS sent successfully.");
                        }
                        else
                        {
                            _logger.LogError("Failed to send opt-out confirmation SMS.");
                        }
                    }
                    else if (message == "HELP")
                    {
                        string helpMessage = "To opt-out of LTI Notifications, text OPT-OUT. Avoid texting STOP to stay connected. If you accidentally text STOP, follow the instructions to opt back in.";
                        if (SendSMS(senderPhoneNumber, helpMessage))
                        {
                            _logger.LogInformation("HELP message SMS sent successfully.");
                        }
                        else
                        {
                            _logger.LogError("Failed to send HELP message SMS.");
                        }
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

        public class SmsReceivedEventData
        {
            public string From { get; set; }
            public string Message { get; set; }
        }
    }
}
