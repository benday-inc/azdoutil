using System.Security.Cryptography;

namespace Benday.AzureDevOpsUtil.Api;


public class RandomNumGen
{
    private readonly RandomNumberGenerator _random;
    // private static ulong biggestNumber = 18446744073709551615; 


    /// <summary> 
    /// Constructor which creates the random number object 
    /// </summary> 
    public RandomNumGen()
    {
        // Create the random number generator. 
        _random = RandomNumberGenerator.Create();
    }


    /// <summary> 
    /// This returns a random number of a particular size 
    /// </summary> 
    /// <param name="bits">Size of the random number in bits</param> 
    /// <returns>Random number</returns> 
    public ulong GetNumber(byte bits)
    {
        ulong randomNumber = 1;
        // Convert the number of bits to bytes. 
        var numBytes = Convert.ToByte(bits / 8);


        var ranbuff = new byte[numBytes];


        // This retrieves a random sequence of bytes. 
        _random.GetBytes(ranbuff);


        uint randomByte;


        // Here we convert the random bytes to a number, using byte 
        // shifting. 
        for (byte i = 0; i < numBytes; i++)
        {
            randomByte = ranbuff[i];
            randomNumber = randomNumber + randomByte *
                Convert.ToUInt64(Math.Pow(2, i * 8));
        }


        // Return the generated number. 
        return randomNumber;
    }


    /// <summary> 
    /// Generates an 8 bit random number 
    /// </summary> 
    /// <returns>Random number</returns> 
    public ulong GetNumber()
    {
        return GetNumber(8);
    }


    /// <summary> 
    /// Returns a random number within a specific range min <= number 
    // < max. 
    /// </summary> 
    /// <param name="min">minimum number of the range</param> 
    /// <param name="max">maximum number of the range</param> 
    /// <returns>random number</returns> 
    public ulong GetNumberInRange(ulong min, ulong max)
    {
        byte bits = 32;


        var randomNumber = GetNumber(bits);
        var biggestNumber = Convert.ToUInt64(Math.Pow(2, bits));


        // Takes the generated number and puts it in the range 
        // that has been asked for. 
        var randomNumberInRange =
            Convert.ToUInt64(Math.Floor(Convert.ToDouble(randomNumber) /
              Convert.ToDouble(biggestNumber) * Convert.ToDouble(max - min) +
              Convert.ToDouble(min)));


        return randomNumberInRange;
    }


    /// <summary> 
    /// Returns a random number within a specific range 
    /// min less than or equal to number less than max 
    /// </summary> 
    /// <param name="min">minimum number of the range</param> 
    /// <param name="max">maximum number of the range</param> 
    /// <returns>random number</returns> 
    public int GetNumberInRange(int min, int max)
    {
        return Convert.ToInt32(GetNumberInRange(Convert.ToUInt64(min),
                                                  Convert.ToUInt64(max)));
    }
}