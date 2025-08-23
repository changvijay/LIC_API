namespace LIC_WebDeskAPI
{
    public class Models
    {
       
    }
    public class HeroSection
    {
        public int? Id { get; set; }
        public string? Title { get; set; }
        public string? Subtitle { get; set; }
        public string? ActionText { get; set; }
        public string? ActionLink { get; set; }
        public string? ImageUrl { get; set; }
        public int? AgentId { get; set; }
    }
    public class Testimonial
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Location { get; set; }
        public string? TestimonialText { get; set; }
        public int? Rating { get; set; }
        public int? AgentId { get; set; }
    }
    public class Blog
    {
        public int? Id { get; set; }
        public int? SortingOrder { get; set; }
        public string? Title { get; set; }
        public string? Excerpt { get; set; }
        public string? ImageUrl { get; set; }
        public string? Category { get; set; }
        public string? Author { get; set; }
        public DateTime? PublishedDate { get; set; }
        public int? AgentId { get; set; }
    }
    public class LifeInsuranceProductRequest
    {
        public string? Title { get; set; }
        public string? Subtitle { get; set; }
        public string? Description { get; set; }
        public string? IconName { get; set; }
        public string? ColorClass { get; set; }
        public string? AgeRange { get; set; }
        public string? MinPremium { get; set; }
        public bool Popular { get; set; }
        public int AgentId { get; set; }
        public List<string> Features { get; set; } = new();
        public List<string> Plans { get; set; } = new();
    }
    public class ContactDetail
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? EmailId { get; set; }
        public string? ServiceRequired { get; set; }
        public string? MessageText { get; set; }
        public int? AgentId { get; set; }
        public string? Status { get; set; }    
    }
    public class Agent
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? paymentStatus { get; set; }
    }

    public class NewsItem
    {
        public string Headline { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
    }


    // Model
    public class PolicyHolderDetail
    {
        public int PolicyHolderId { get; set; }
        public string PolicyHolderName { get; set; }
        public string ContactNumber { get; set; }
        public string PolicyName { get; set; }
        public decimal AmountPerCycle { get; set; }
        public string PaymentCycle { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public int AgentId { get; set; }
        public string Remarks { get; set; }
    }
}
