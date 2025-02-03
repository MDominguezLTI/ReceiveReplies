using System;
using System.IO;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Azure.Communication.Sms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Messaging.EventGrid;

public static class ReceiveSmsFunction
{
	private static readonly HttpClient _httpClient = new HttpClient();

	[FunctionName("ReceiveSmsFunction")]
	public static async Task<IActionResult> Run(
		[HttpTrigger(AuthorizationLevel.Function, "post", Route = "notifications/received-reply")] HttpRequest req,
		ILogger log)
	{
		log.LogInformation("Received an SMS notification.");

		string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
		var events = JsonConvert.DeserializeObject<EventGridEvent[]>(requestBody);

		foreach (var eventGridEvent in events)
		{
			if (eventGridEvent.EventType == "Microsoft.Communication.SMSReceived")
			{
				var smsMessage = JsonConvert.DeserializeObject<SmsReceivedEventData>(eventGridEvent.Data.ToString());
				string sentPhoneNumber = smsMessage.From;
				string phoneNumber = sentPhoneNumber.Substring(2);

				log.LogInformation($"Received SMS from {sentPhoneNumber}: {smsMessage.Message}");

				if (smsMessage.Message.Trim().ToUpper() == "STOP")
				{

					// Send confirmation SMS
					bool isSMSSent = await SendSMSAsync(sentPhoneNumber, "You have been successfully opted-out of notifications!");
					if (!isSMSSent)
					{
						return new BadRequestObjectResult("Error: SMS sending failed!");
					}
				}
				else
				{
					return new BadRequestObjectResult("Invalid Message Received");
				}
			}
		}

		return new OkObjectResult("Opted Out Successfully!");
	}

	// Simulated method to send SMS confirmation
	private static async Task<bool> SendSMSAsync(string recipient, string message)
	{
		try
		{
			SmsClient smsClient = new SmsClient("endpoint=https://ltiazurecommsresource.unitedstates.communication.azure.com/;accesskey=8DSes5xa4F1dvyFFJCMQUPxFjCU5jWTOT6vSpQnLZHPhkEcwVRh7JQQJ99AKACULyCpUrFFTAAAAAZCSPXtN");
			var response = await smsClient.SendAsync(
				from: "+18772246875", // Replace with your sender number
				to: recipient,
				message: message
			);
			return response.Value.Successful;
		}
		catch (Exception)
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
