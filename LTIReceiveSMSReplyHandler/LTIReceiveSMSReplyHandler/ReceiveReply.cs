using System;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Communication.Sms;
using Newtonsoft.Json;

namespace FunctionApp1
{
	public class SMSReceiveReply
	{
		private readonly ILogger<SMSReceiveReply> _logger;
		private readonly SmsClient _smsClient;

		public SMSReceiveReply(ILogger<SMSReceiveReply> logger)
		{
			_logger = logger;
			// Initialize the SMS Client with your Azure Communication Services connection string
			_smsClient = new SmsClient("endpoint=https://ltiazurecommsresource.unitedstates.communication.azure.com/;accesskey=8DSes5xa4F1dvyFFJCMQUPxFjCU5jWTOT6vSpQnLZHPhkEcwVRh7JQQJ99AKACULyCpUrFFTAAAAAZCSPXtN");
		}

		[Function(nameof(SMSReceiveReply))]
		public void Run([EventGridTrigger] CloudEvent cloudEvent)
		{
			_logger.LogInformation("Event type: {type}, Event subject: {subject}", cloudEvent.Type, cloudEvent.Subject);

			// Check if the event is an SMS received event
			if (cloudEvent.Type == "Microsoft.Communication.SMSReceived")
			{
				// Deserialize the event data into the SMS data object
				var smsEvent = JsonConvert.DeserializeObject<SmsReceivedEventData>(cloudEvent.Data.ToString());

				if (smsEvent != null)
				{
					string senderPhoneNumber = smsEvent.From;
					string message = smsEvent.Message?.Trim().ToUpper();  // Trim and convert to upper case

					_logger.LogInformation($"Received SMS from {senderPhoneNumber}: {message}");

					// Check if the message is "STOP"
					if (message == "OPT-OUT")
					{
						// Send opt-out confirmation message
						string optOutMessage = "You have been successfully opted-out of notifications!";
						var isMessageSent = SendSMS(senderPhoneNumber, optOutMessage);

						if (isMessageSent)
						{
							_logger.LogInformation("Opt-out confirmation SMS sent successfully.");
						}
						else
						{
							_logger.LogError("Failed to send opt-out confirmation SMS.");
						}
					}
					else
					{
						// Send "We received your message" response
						string responseMessage = $"We received your message: '{smsEvent.Message}'";
						var isMessageSent = SendSMS(senderPhoneNumber, responseMessage);

						if (isMessageSent)
						{
							_logger.LogInformation("Response SMS sent successfully.");
						}
						else
						{
							_logger.LogError("Failed to send response SMS.");
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

		// Method to send SMS response back to the user
		private bool SendSMS(string recipient, string message)
		{
			try
			{
				// Replace with your actual sender number
				var response = _smsClient.SendAsync(
					from: "+18772246875",  // Your sender number here
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

		// Class to handle the deserialized event data
		public class SmsReceivedEventData
		{
			public string From { get; set; }
			public string Message { get; set; }
		}
	}
}
