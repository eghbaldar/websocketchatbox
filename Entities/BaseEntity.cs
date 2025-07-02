namespace websocket.Entities
{
    public class BaseEntity
    {
        public Guid Id { get; set; }
        public DateTime InsertDate { get; set; }= DateTime.Now;
        public DateTime? UpdateDate { get; set; }
        public DateTime? DeleteDate { get; set; }
    }
}
