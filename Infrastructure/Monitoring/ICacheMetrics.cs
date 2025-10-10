namespace Monitoring;

public interface ICacheMetrics
{
    void Hit(string name);
    void Miss(string name);
}
