using System.Data.Common;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Accusing : MonoBehaviour
{
    [Header("Interaction")]

    public float AccuseRange = 3f;

    
    private CustomerController closestCustomer;

    private void Update()
    {
        FindClosestCustomer();
    }

    private void FindClosestCustomer()
    {
        CustomerController[] customers = 
            FindObjectsByType<CustomerController>();

        CustomerController nearest = null;
        float closestDistance = AccuseRange;

        foreach (CustomerController customer in customers)
        {
            float distance = Vector3.Distance(
                transform.position,
                customer.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearest = customer;
            }
        }

        if (nearest != closestCustomer)
        {
            if (closestCustomer != null)
            {
                closestCustomer.AccuseButton.SetActive(false);
            }

            closestCustomer = nearest;

            if (closestCustomer != null)
            {
                closestCustomer.AccuseButton.SetActive(true);
            }
        }
    }



    public void AccuseCustomer()
    {
        if (closestCustomer == null)
            return;

        CustomerController accusedCustomer = closestCustomer;

        accusedCustomer.Accused();

        accusedCustomer.AccuseButton.SetActive(false);

        closestCustomer = null;


    }











}
