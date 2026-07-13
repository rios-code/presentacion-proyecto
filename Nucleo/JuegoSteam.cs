namespace Steam
{
    // Un juego de la biblioteca del usuario en Steam.
    public record JuegoSteam(int AppId, string Nombre, int MinutosJugados)
    {
        public int HorasJugadas => MinutosJugados / 60;
    }
}
