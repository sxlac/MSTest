package helpers.data;
import java.util.Random;

/**
 * Faker Data Generators
 */
public class Faker extends com.github.javafaker.Faker {
    public String firstName() {
        return super.name().firstName();
    }

    public String lastName() {
        return super.name().lastName();
    }

    public String birthday() {
        return super.date().birthday().toString();
    }

    public String birthday(int minAge, int maxAge) {
        return super.date().birthday(minAge, maxAge).toString();
    }

    public String randomDigit() {
        return String.valueOf(super.number().randomNumber());
    }

    public String randomDigit(int numberOfDigits) {
        return String.valueOf(super.number().randomNumber(numberOfDigits, true));
    }

    public String randomQuote() {
        return super.hobbit().quote();
    }
    
    public char randomChar() {
        return (char) ('A' + super.random().nextInt(26));
    }

    /**
     * Returns random upper case letter from from A to Z
     * 
     * @return The desired random upper case letter.
     */
    public String randomLetter() {
        Random random = new Random();
        char randomChar = (char) (random.nextInt(26) + 'a');
        String randomString = String.valueOf(randomChar); 
        return randomString.toUpperCase();
    }

    /**
     * Returns randomCenseoId  e.g.
     * Z7350586
     * 
     * @param lenghOfDigit lengh Of random generated digit
     * @return The desired randomCenseoId 
     *
     */
    public String randomCenseoId(int lenghOfDigit) {
        return String.format("%s%s", randomLetter(), randomDigit(lenghOfDigit));
    }    
}
