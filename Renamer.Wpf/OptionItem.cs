namespace Com.Scm.Renamer
{
    public class OptionItem<T>
    {
        public T Value;
        public string Text;

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (!(obj is T))
            {
                return false;
            }

            return Equals((OptionItem<T>)obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    public class EnumOptionItem<T> : OptionItem<T>
    {
    }
}
