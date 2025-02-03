using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Communication.Sms;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using System.Net.Http;
using System.Net.Mail;

public static class ReceiveSmsFunction
{
	private static readonly HttpClient _httpClient = new HttpClient();

	// EventGridTrigger to listen to Event Grid events
	[FunctionName("ReceiveSmsFunction")]
	public static async Task Run(
		[EventGridTrigger] EventGridEvent eventGridEvent, // EventGridTrigger to bind event data
		ILogger log)
	{
		log.LogInformation("Received an SMS notification.");

		// Deserialize the event data
		if (eventGridEvent.EventType == "Microsoft.Communication.SMSReceived")
		{
			var smsMessage = JsonConvert.DeserializeObject<SmsReceivedEventData>(eventGridEvent.Data.ToString());
			string sentPhoneNumber = smsMessage.From;
			string phoneNumber = sentPhoneNumber.Substring(2);  // Adjust phone number format as needed

			log.LogInformation($"Received SMS from {sentPhoneNumber}: {smsMessage.Message}");

			// Check if the message is "STOP" and send a confirmation SMS
			if (smsMessage.Message.Trim().ToUpper() == "STOP")
			{
				bool isSMSSent = await SendSMSAsync(sentPhoneNumber, "You have been successfully opted-out of notifications!");
				if (!isSMSSent)
				{
					log.LogError("Error: SMS sending failed!");
				}
			}
			else
			{
				log.LogWarning("Invalid Message Received");
			}
		}
		else
		{
			log.LogWarning($"Unhandled Event Type: {eventGridEvent.EventType}");
		}
	}

	// Method to send SMS confirmation
	private static async Task<bool> SendSMSAsync(string recipient, string message)
	{
		try
		{
			// Create SMS client with connection string and send the message
			SmsClient smsClient = new SmsClient("endpoint=https://ltiazurecommsresource.unitedstates.communication.azure.com/;accesskey=YOUR_ACCESS_KEY");
			var response = await smsClient.SendAsync(
				from: "+18772246875", // Replace with your sender number
				to: recipient,
				message: message
			);
			return response.Value.Successful;
		}
		catch (Exception ex)
		{
			return false;
		}
	}

	// Class to handle the SMS event data
	public class SmsReceivedEventData
	{
		public string From { get; set; }
		public string Message { get; set; }
	}
}
