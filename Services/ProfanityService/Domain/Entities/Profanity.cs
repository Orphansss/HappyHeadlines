namespace ProfanityService.Domain.Entities;
public class Profanity
{
    public int Id { get; set; }
    public string Term { get; set; }
    public string NormalizedTerm { get; set; }

    //Simple toggle to check if the term need to be used. 
    public bool IsActive { get; set; } = true;
    //How offensive a term is: Mild(0), Moderate(1), Severe(3).
    public int Serverity { get; set; } = 1;
}
