namespace Monitoring;

public interface ICacheMetrics
{
    void Hit(string name);
    void Miss(string name);
    void SetSize(string name, int size);
    void Evict(string name);
}
