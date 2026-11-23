using System;

namespace Blockganizer
{
    internal sealed class MotivationalQuote
    {
        public readonly string Text, Author, Source;

        public MotivationalQuote(string text, string author, string source)
        {
            Text = text;
            Author = author;
            Source = source;
        }
    }

    internal static class MotivationalQuotes
    {
        // Foreign-language excerpts are translated for the Spanish interface.
        private static readonly MotivationalQuote[] Quotes =
        {
            new MotivationalQuote("Caminante, no hay camino, se hace camino al andar.", "Antonio Machado", "Proverbios y cantares, XXIX"),
            new MotivationalQuote("Sigo haciendo lo que todavía no sé hacer para aprender a hacerlo.", "Vincent van Gogh", "Carta 528 a Anthon van Rappard"),
            new MotivationalQuote("Nada grande se crea de repente.", "Epicteto", "Disertaciones, I.15"),
            new MotivationalQuote("Lo que sólo he deseado es estudiar para ignorar menos.", "Sor Juana Inés de la Cruz", "Respuesta a Sor Filotea de la Cruz"),
            new MotivationalQuote("Lo que hoy se ha demostrado, antes sólo se imaginaba.", "William Blake", "El matrimonio del cielo y el infierno"),
            new MotivationalQuote("Mientras aplazamos las cosas, la vida se nos escapa.", "Séneca", "Cartas a Lucilio, I"),
            new MotivationalQuote("Solos podemos hacer tan poco; juntos podemos hacer tanto.", "Helen Keller", "Discurso en Washington, 1925"),
            new MotivationalQuote("Un viaje de mil millas comienza con un solo paso.", "Lao Tse", "Tao te ching, 64"),
            new MotivationalQuote("Nada más necesitas paciencia. O dale un nombre más fascinante y llámala esperanza.", "Jane Austen", "Sentido y sensibilidad"),
            new MotivationalQuote("Si no hay lucha, no hay progreso.", "Frederick Douglass", "Discurso sobre la emancipación en las Indias Occidentales, 1857"),
            new MotivationalQuote("Confía en ti: cada corazón vibra con esa cuerda de hierro.", "Ralph Waldo Emerson", "Confianza en uno mismo"),
            new MotivationalQuote("La mejor salida siempre es atravesar la dificultad.", "Robert Frost", "A Servant to Servants"),
            new MotivationalQuote("No me asustan las tormentas, porque estoy aprendiendo a navegar mi propio barco.", "Louisa May Alcott", "Mujercitas"),
            new MotivationalQuote("Hay cosas que dependen de nosotros y cosas que no.", "Epicteto", "Manual, I"),
            new MotivationalQuote("La esperanza es esa cosa con plumas.", "Emily Dickinson", "Poema 314"),
            new MotivationalQuote("Nuestra vida se nos va en detalles. ¡Simplifica, simplifica!", "Henry David Thoreau", "Walden, «Dónde viví y para qué»"),
            new MotivationalQuote("Lo bien hecho vale más que lo bien dicho.", "Benjamin Franklin", "Almanaque del pobre Richard, 1737"),
            new MotivationalQuote("Si supiéramos dónde estamos y hacia dónde vamos, decidiríamos mejor qué hacer y cómo hacerlo.", "Abraham Lincoln", "Discurso de la casa dividida, 1858"),
            new MotivationalQuote("El tiempo perdido nunca se recupera.", "Benjamin Franklin", "El camino hacia la riqueza"),
            new MotivationalQuote("Avanza con confianza en la dirección de tus sueños.", "Henry David Thoreau", "Walden, conclusión (adaptación)")
        };

        public static int Count { get { return Quotes.Length; } }

        public static MotivationalQuote ForDay(DateTime date, int offset)
        {
            long day = date.Date.Ticks / TimeSpan.TicksPerDay;
            return Quotes[(day + offset) % Quotes.Length];
        }
    }
}
