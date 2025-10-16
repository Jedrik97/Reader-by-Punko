using UnityEngine;
using System.Threading.Tasks;

public class App : MonoBehaviour
{
    public OpenFlow openFlow;

    public async void OnExternalOpen(string uriStr)
    {
        await openFlow.HandleExternalUriAsync(uriStr);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }
}