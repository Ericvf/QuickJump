using System.Threading.Tasks;

namespace QuickJump
{
    public interface IItemLauncher
    {
        Task LaunchItem(Item item);
    }
}
