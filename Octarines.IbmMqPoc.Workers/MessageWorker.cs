using IBM.WMQ;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octarines.IbmMqPoc.Models.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Octarines.IbmMqPoc.Workers
{
    public class MessageWorker : BackgroundService
    {
        private readonly ILogger<MessageWorker> _logger;
        private readonly MqSettings _mqSettings;
        private readonly Hashtable _connectionProperties = null;


        public MessageWorker(
            ILogger<MessageWorker> logger,
            IOptions<MqSettings> mqSettings)
        {
            _logger = logger;
            _mqSettings = mqSettings.Value;

            _connectionProperties = new Hashtable()
            {
                { MQC.HOST_NAME_PROPERTY, _mqSettings.HostIPAddress },
                { MQC.PORT_PROPERTY, _mqSettings.PortNumber },
                { MQC.CHANNEL_PROPERTY, _mqSettings.ClientChannelId }
            };

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"IBM MQ PoC {nameof(MessageWorker)} running at: {DateTime.Now}", "IBM MQ PoC");
                await StartMessageHandler(stoppingToken);
            }
        }

        private async Task StartMessageHandler(CancellationToken stoppingToken)
        {
            try
            {
                MQQueueManager queueManager = new MQQueueManager(_mqSettings.QueueManager, _connectionProperties);
                MQQueue queue = queueManager.AccessQueue(_mqSettings.QueueName, MQC.MQOO_INPUT_AS_Q_DEF + MQC.MQOO_FAIL_IF_QUIESCING);
                while (!stoppingToken.IsCancellationRequested)
                {
                    MQMessage queueMessage;
                    MQGetMessageOptions queueGetMessageOptions;
                    string message = string.Empty;
                    try
                    {
                        queueMessage = new MQMessage()
                        {
                            Format = MQC.MQFMT_STRING
                        };

                        queueGetMessageOptions = new MQGetMessageOptions { WaitInterval = 60 * 1000 };
                        queueGetMessageOptions.Options |= MQC.MQGMO_WAIT;
                        queueGetMessageOptions.Options |= MQC.MQGMO_SYNCPOINT;

                        _logger.LogInformation($"Waiting for message");

                        queue.Get(queueMessage, queueGetMessageOptions);

                        if (queueMessage.Format.CompareTo(MQC.MQFMT_STRING) == 0)
                        {
                            message = queueMessage.ReadString(queueMessage.MessageLength);
                        }
                        else
                        {
                            _logger.LogInformation($"UNRECOGNISED MESSAGE FORMAT");
                        }

                        _logger.LogInformation($"Raw Message {message}");

                        if (!string.IsNullOrEmpty(message)) // additional validation logic here
                        {
                            _logger.LogInformation($"Valid Message {message}");

                            // do stuff with the message
                        }
                        else
                        {
                            _logger.LogWarning($"Invalid Message");

                            // send message to dlq
                        }

                    }
                    catch (MQException mqex)
                    {
                        if (mqex.Reason == MQC.MQRC_NO_MSG_AVAILABLE)
                        {
                            _logger.LogInformation($"Warn {mqex.ReasonCode} {mqex.Message}");
                        }
                        else
                        {
                            _logger.LogError("IBM MQ Consumer Error", $" Error in Consuming the Message {mqex.Reason} {mqex.Message} {mqex.StackTrace}");
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Uknown error consuming message from queue {message}", e.Message, nameof(MessageWorker));
                    }
                    finally
                    {
                        if (queueManager != null)
                        {
                            queueManager.Commit();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error In StartMessageHandler {message}", e.Message, nameof(MessageWorker));
            }
        }
    }
}
