#define Graph_And_Chart_PRO
using UnityEngine;
using ChartAndGraph;
using System.Collections;

public class GraphChartFeed : MonoBehaviour
{
	void Start ()
    {
        GraphChart graph = GetComponent<GraphChart>();

        if(graph != null)
        {
            graph.DataSource.StartBatch();
            graph.DataSource.ClearCategory("Acceleration");
            graph.DataSource.ClearCategory("AngularVelocity");
            graph.DataSource.ClearCategory("Angle");
            graph.DataSource.ClearCategory("Temperature");
            graph.DataSource.ClearCategory("GPSLongitude");

            for(int i=0; i<30; i++)
            {
                graph.DataSource.AddPointToCategory("Acceleration", Random.value * 10, Random.value * 10);
                graph.DataSource.AddPointToCategory("AngularVelocity", Random.value * 10, Random.value * 10);
                graph.DataSource.AddPointToCategory("Angle", Random.value * 10, Random.value * 10);
                graph.DataSource.AddPointToCategory("Temperature", Random.value * 10, Random.value * 10);
                graph.DataSource.AddPointToCategory("GPSLongitude", Random.value * 10, Random.value * 10);
            }

            graph.DataSource.EndBatch();
        }
       // StartCoroutine(ClearAll());
    }

    IEnumerator ClearAll()
    {
        yield return new WaitForSeconds(5f);
        GraphChartBase graph = GetComponent<GraphChartBase>();

        graph.DataSource.Clear();
    }
}
