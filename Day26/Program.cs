using System;
using System.Transactions;
using System.Data.SqlClient;
using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;

namespace BuzzFeed
{
    class Program
    {
        static void Main(string[] args)
        {
            //SqlConnection connection = new SqlConnection(@"Server=minecraft.lfgpgh.com;Database=buzzfeed_2;User Id=academy_student;Password=12345;" + "MultipleActiveResultSets=true;");

            //TO DO - needs classes and functions
            //TO DO - convert score to a percentage in take

            //Added MARS to the connection string which allows for multiple DataReaders to be open simultaneously...
            //Comes in VERY handy for displaying the questions/answers in take-a-test mode 
            //Did a lot of test runs on personal tables so I didn't make a huge mess out of our database on the server

            SqlConnection connection = new SqlConnection(@"Data Source=(LocalDb)\MSSQLLocalDB;AttachDbFilename=C:\Users\JDAVI\OneDrive\Documents\BACKUPS\ACADEMYPGH\WEEK6\Day26\Day26BuzzFeed\Day26\Database1.mdf;Integrated Security=True;" + "MultipleActiveResultSets=true;");

            connection.Open();

            string userTask = "";
            while (userTask != "q")
            {
                Console.WriteLine("Welcome to BuzzFeed! Would you like to (m)ake a quiz, (t)ake a quiz, or (q)uit?");
                userTask = Console.ReadLine().ToLower();
                if (userTask == "q")
                {
                    Console.WriteLine("Goodbye!");
                }
                else if (userTask == "m")
                {
                    Console.WriteLine("Hello! Welcome to the quiz maker.\n\nDO NOT EVER USE AN APOSTROPHE!\n\nWhat would you like to name your quiz?");
                    string quizName = Console.ReadLine();

                    //Inserting into Quizzes and getting the Quiz ID simultaneously, will be necessary for linking questions and results in a bit
                    SqlCommand newQuiz = new SqlCommand($"INSERT INTO [Quizzes] (Name) VALUES ('{quizName}'); SELECT @@Identity AS ID", connection);
                    SqlDataReader reader = newQuiz.ExecuteReader();
                    reader.Read();
                    int quizID = Convert.ToInt32(reader["ID"]);
                    reader.Close();

                    //Making for loops for results, questions and answers, simple enough to have the user choose the quantity of each
                    //Was also really helpful for making dummy tests - anything above just a couple questions/answers/results was very annoying to deal with
                    Console.WriteLine("Let's enter the possible results first. How many would you like to have?");
                    int resultQty = Convert.ToInt32(Console.ReadLine());

                    for (int i = 1; i <= resultQty; i++)
                    {
                        //Getting the desired results...
                        Console.WriteLine($"Enter the title of result #{i}");
                        string result = Console.ReadLine();
                        Console.WriteLine("Enter the text for this result.");
                        string flavor = Console.ReadLine();
                        Console.WriteLine("How many points to achieve this result?");
                        int threshold = Convert.ToInt32(Console.ReadLine());

                        //We interpreted that the results would be on a scale of 0-100 (or whatever the test-maker wanted as long as it's a numerical range)
                        //and that the "points total" values would be the thresholds for a new result with calculation occuring in take-a-test such that
                        //any number of results, questions, answers and point values should work
                        SqlCommand newResult = new SqlCommand($"INSERT INTO [Results] (Title, Text, TotalPoints, QuizId) VALUES ('{result}', '{flavor}', '{threshold}', {quizID})", connection);
                        newResult.ExecuteNonQuery();
                    }

                    Console.WriteLine("Let's work on the questions and answers next. How many questions do you want to have?");
                    int questionQty = Convert.ToInt32(Console.ReadLine());
                    Console.WriteLine("And how many answers per question?");
                    int answerQty = Convert.ToInt32(Console.ReadLine());

                    for (int i = 1; i <= questionQty; i++)
                    {
                        //Getting the desired questions, plus the Question ID so we can link answers directly do them...
                        Console.WriteLine($"Enter the text for question #{i}:");
                        string question = Console.ReadLine();
                        SqlCommand newQuestion = new SqlCommand($"INSERT INTO [Questions] (Prompt, QuizId) VALUES ('{question}', {quizID}); SELECT @@Identity AS ID", connection);
                        SqlDataReader reader2 = newQuestion.ExecuteReader();
                        reader2.Read();
                        int questionID = Convert.ToInt32(reader2["ID"]);
                        reader2.Close();

                        for (int x = 1; x <= answerQty; x++)
                        {
                            //And another for loop in a for loop so the users can add all answers to each questions sequentially
                            Console.WriteLine($"Enter answer #{x} for question #{i}:");
                            string answer = Console.ReadLine();
                            Console.WriteLine("Enter the number of points for this answer.");
                            int points = Convert.ToInt32(Console.ReadLine());
                            SqlCommand newAnswer = new SqlCommand($"INSERT INTO [Answers] (Body, Points, QuestionId) VALUES ('{answer}', {points}, {questionID})", connection);
                            newAnswer.ExecuteNonQuery();
                        }
                    }
                    Console.WriteLine("Thanks for submitting a test! Returning to main menu.");
                }
                else if (userTask == "t")
                {
                    //Just grabbing a username to associate with a userID unique to each individual quiz attempt...
                    Console.WriteLine("Welcome to BuzzFeed! What is your username?");
                    string userName = Console.ReadLine();

                    //Displaying all possible quizzes for user selection, choice made by quizID itself
                    Console.WriteLine($"Hi, {userName}! Which quiz do you want to take?");
                    SqlCommand quizList = new SqlCommand($"SELECT * FROM Quizzes", connection);
                    SqlDataReader reader = quizList.ExecuteReader();
                    while (reader.Read())
                    {
                        Console.Write(reader["Id"] + " / ");
                        Console.WriteLine(reader["Name"]);
                    }
                    reader.Close();

                    int quizChoice = Convert.ToInt32(Console.ReadLine());

                    //Saving info for this quiz attempt including quiz, name, and returning the userID to track answers and points later
                    SqlCommand saveUser = new SqlCommand($"INSERT INTO [Users] (Name, QuizId) VALUES ('{userName}', {quizChoice}); SELECT @@Identity AS ID", connection);
                    SqlDataReader reader2 = saveUser.ExecuteReader();
                    reader2.Read();
                    int userID = Convert.ToInt32(reader2["ID"]);
                    reader2.Close();

                    //Starting a loop to display all the quiz questions that match the current quizID
                    SqlCommand quizQuestions = new SqlCommand($"SELECT * FROM [Questions] WHERE QuizId = {quizChoice}", connection);
                    SqlDataReader reader3 = quizQuestions.ExecuteReader();
                    int questionId;
                    int answerChoice;
                    while (reader3.Read())
                    {
                        Console.WriteLine(reader3["Prompt"]);
                        questionId = (int)reader3["Id"];

                        //Starting another loop to display all the answers per question, using x as a counter to number them, resets at each new question
                        SqlCommand quizAnswers = new SqlCommand($"SELECT * FROM [Answers] WHERE QuestionId = {questionId}", connection);
                        SqlDataReader reader4 = quizAnswers.ExecuteReader();
                        int x = 1;
                        while (reader4.Read())
                        {
                            Console.Write(x + " / ");
                            Console.WriteLine(reader4["Body"]);
                            x = x + 1;
                        }
                        reader4.Close();

                        //Some messy code to help choose the correct answer based on user input...
                        //If the user can choose from 1-4 but the actual answerIDs are 88-92, find the value of the FIRST answerID associated with
                        //the question (88), then add the value of the user's input minus 1 to prevent off-by-one error (1 becomes 88, 2 becomes 89, etc.)
                        answerChoice = Convert.ToInt32(Console.ReadLine());
                        SqlCommand answerGrab = new SqlCommand($"SELECT (Id) FROM [Answers] WHERE QuestionId = {questionId}", connection);
                        SqlDataReader reader5 = answerGrab.ExecuteReader();
                        reader5.Read();
                        int answerID = Convert.ToInt32(reader5["Id"]) + (answerChoice - 1);
                        reader5.Close();

                        SqlCommand answerStore = new SqlCommand($"INSERT INTO [UsersAnswers] (UserId, AnswerId) VALUES ({userID}, {answerID})", connection);
                        answerStore.ExecuteNonQuery();
                    }
                    reader3.Close();

                    //Ready to calculate user score... probably could have made a join command for this but it started to get messy and now we can
                    //do multiple reader loops simulatenously so just all the answers the user has chosen by comparing userID...
                    int pointTotals = 0;
                    SqlCommand findAnswers = new SqlCommand($"SELECT (AnswerId) FROM [UsersAnswers] WHERE UserId = ({userID})", connection);
                    SqlDataReader finder = findAnswers.ExecuteReader();
                    while (finder.Read())
                    {
                        //...then grab the point values that match those answers by checking each one and adding the value to a running tally on each pass
                        int answerID = (int)finder["AnswerId"];
                        SqlCommand findPoints = new SqlCommand($"SELECT (Points) FROM [Answers] WHERE Id = ({answerID})", connection);
                        SqlDataReader finder2 = findPoints.ExecuteReader();
                        while (finder2.Read())
                        {
                            pointTotals = pointTotals + (int)finder2["Points"];
                        }
                        finder2.Close();
                    }
                    finder.Close();

                    //TO-DO a little quick math to represent the user's point total in the "congrats" line either a "XX out of YY" or a percentage...
                    //Scan each question, find the highest point answer for each, combine them for the highest possible score then drop in as is or divide
                    SqlCommand findResult = new SqlCommand($"SELECT * FROM [Results] WHERE QuizId = {quizChoice} AND TotalPoints <= {pointTotals} ORDER BY TotalPoints DESC", connection);
                    SqlDataReader finder3 = findResult.ExecuteReader();
                    finder3.Read();
                    Console.WriteLine($"Congratulations, {userName}! You scored a {pointTotals}! Here are your results:");
                    Console.WriteLine(finder3["Title"]);
                    Console.WriteLine(finder3["Text"]);
                    finder3.Close();
                    Console.WriteLine("Press enter to return to main menu.");
                    Console.ReadLine();
                }
                else
                {
                    Console.WriteLine("Invalid entry. Try Again.");
                }
            }
            connection.Close();
        }
    }
}

//HOW TO DO QUESTION/ANSWER WITHOUT BREAKING THE FRAMEWORK AND FORCING MULTIPLE LOOPS/READERS... KINDA MESSY THOUGH...
//cmd = new SqlCommand("SELECT *, Answers.Id AS AmzerId  FROM [Questions] JOIN Answers ON Questions.Id = Answers.QuestionId WHERE QuizId=" + quizId, connection);
//SqlDataReader questionReader = cmd.ExecuteReader();
//bool firstTimeShowingQuestion = true;
//bool isVeryVeryFirstTime = true;
//List<string> answers = new List<string>();
//string previousQuestion = "";
//while (questionReader.Read())
//{
//    // hey, if we are on a new question
//    // can we set firstTimeShowingQuestion to true again
//    if (questionReader["Prompt"].ToString() != previousQuestion)
//    {
//        if (!isVeryVeryFirstTime)
//        {
//            Console.WriteLine("What is your answer?");
//            answers.Add(Console.ReadLine());
//        }
//        firstTimeShowingQuestion = true;
//        previousQuestion = questionReader["Prompt"].ToString();
//    }
//    if (firstTimeShowingQuestion)
//    {
//        // if it's the first time we're seeing this question
//        // print out the question
//        Console.WriteLine(questionReader["Prompt"]);
//        firstTimeShowingQuestion = false;
//    }
//    // print out the answers
//    Console.WriteLine(questionReader["AmzerId"] + ")   " + questionReader["Body"]);

//    isVeryVeryFirstTime = false;
//}

//HOW TO DO IT ALL WITH CLASSES, HUGE IMPROVEMENT!!
//using System;
//using System.Collections.Generic;
//using System.Data.SqlClient;

//namespace BuzzfeedTaker
//{
//    class Program
//    {
//        static void Main(string[] args)
//        {
//            // Take a quiz
//            // First, list out all of the quizzes
//            // Let the user pick which quiz to do
//            // Show the first question
//            // Show the list of answers possible from that question
//            // Get the user's answer
//            // Save it
//            // Repeat for the next questions until done
//            // Tally their score, show the right result

//            // Setup my database so I can get them quizzes
//            SqlConnection connection = new SqlConnection(@"Data Source=minecraft.lfgpgh.com;Initial Catalog=Buzzfeed_5;User ID=academy_student;Password=12345");
//            connection.Open();
//            SqlCommand cmd;
//            SqlDataReader reader;

//            // List all of the quizzes
//            cmd = new SqlCommand("SELECT * FROM Quizzes", connection);

//            reader = cmd.ExecuteReader();
//            while (reader.Read())
//            {
//                Console.WriteLine(reader["Id"] + ") " + reader["Name"]);
//            }
//            reader.Close();

//            // Let the user pick which one they want to take
//            Console.WriteLine("Which one do you want to take?");
//            string quizId = Console.ReadLine();

//            // Load up the quiz class we created so we can use it
//            cmd = new SqlCommand("SELECT * FROM Quizzes WHERE Id=" + quizId, connection);
//            reader = cmd.ExecuteReader();
//            reader.Read(); // only reading one row, don't need a loop
//            Quiz myQuiz = new Quiz();
//            myQuiz.Id = Convert.ToInt32(reader["Id"]);
//            myQuiz.Name = reader["Name"].ToString();
//            myQuiz.Questions = new List<Question>();
//            // just like in ruby going myarray = []
//            reader.Close();

//            cmd = new SqlCommand("SELECT * FROM Questions WHERE QuizId=" + myQuiz.Id, connection);
//            reader = cmd.ExecuteReader();
//            while (reader.Read()) // who knows how many questions, maybe lots!
//            {
//                Question currentQuestion = new Question();
//                currentQuestion.Id = Convert.ToInt32(reader["Id"]);
//                currentQuestion.Prompt = reader["Prompt"].ToString();
//                currentQuestion.Answers = new List<Answer>();
//                myQuiz.Questions.Add(currentQuestion);
//            }
//            reader.Close();

//            // myQuest.Questions.each do |question| in ruby
//            foreach (Question question in myQuiz.Questions)
//            {
//                cmd = new SqlCommand("SELECT * FROM Answers WHERE QuestionId=" + question.Id, connection);
//                reader = cmd.ExecuteReader();
//                while (reader.Read()) // potentially more than one answer
//                {
//                    Answer currentAnswer = new Answer();
//                    currentAnswer.Id = Convert.ToInt32(reader["Id"]);
//                    currentAnswer.Points = Convert.ToInt32(reader["Points"]);
//                    currentAnswer.Body = reader["Body"].ToString();
//                    question.Answers.Add(currentAnswer);
//                }
//                reader.Close();
//            }
//            // Ok, finally done loading up that quiz class
//            // and it is ready to use, whew

//            // Show the first question
//            // Show the list of answers possible from that question
//            // Get the user's answer
//            // Save it
//            // Repeat for the next questions until done
//            foreach (Question question in myQuiz.Questions)
//            {
//                // Show the question
//                Console.WriteLine("The question is: " + question.Prompt);
//                // Show all possible answers
//                foreach (Answer answer in question.Answers)
//                {
//                    Console.WriteLine(answer.Id + ") " + answer.Body);
//                }
//                // Save user's answer
//                Console.WriteLine("What is your answer?");
//                string userChoice = Console.ReadLine();
//            }

//            // loop over answers
//            // for each one, do an insert
//            // INSERT INTO UserAnswers (UserId, AnswerID) VALUES ({SomeUserId}, {CurrentAnswerId})

//            // read the final score of the quiz, show the right result
//            reader.Close();

//            connection.Close();
//        }
//    }

//    public class Quiz
//    {
//        public int Id;
//        public string Name;
//        public List<Question> Questions;
//    }

//    public class Question
//    {
//        public int Id;
//        public string Prompt;
//        public List<Answer> Answers;
//    }

//    public class Answer
//    {
//        public int Points;
//        public int Id;
//        public string Body;
//    }
//}
