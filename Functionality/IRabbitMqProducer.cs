namespace MultiChainAPI.Functionality
{
    public interface IRabbitMqProducer
    {

        Task SendDataToQueue(object message,string queuename);
    }
}
