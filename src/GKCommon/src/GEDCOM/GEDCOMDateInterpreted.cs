namespace GKCommon.GEDCOM
{
	public sealed class GEDCOMDateInterpreted : GEDCOMDate
	{
		private string fDatePhrase;

		public string DatePhrase
		{
			get	{ return this.fDatePhrase; }
			set	{ this.SetDatePhrase(value); }
		}

		private void SetDatePhrase(string value)
		{
			string phrase = value;
			if (!string.IsNullOrEmpty(phrase))
			{
				if (phrase[0] == '(')
				{
					phrase = phrase.Remove(0, 1);
				}

				if (phrase.Length > 0 && phrase[phrase.Length - 1] == ')')
				{
					phrase = phrase.Remove(phrase.Length - 1, 1);
				}
			}
			
			this.fDatePhrase = phrase;
		}

		protected override void CreateObj(GEDCOMTree owner, GEDCOMObject parent)
		{
			base.CreateObj(owner, parent);
			this.fDatePhrase = "";
		}

		protected override string GetStringValue()
		{
			return ("INT " + base.GetStringValue() + " " + "(" + this.fDatePhrase + ")");
		}

		private string ExtractPhrase(string str)
		{
			string result = str;
			if (result.Length >= 2 && result[0] == '(')
			{
				result = result.Remove(0, 1);

				int c = 0;
				int num = result.Length;
				for (int I = 1; I <= num; I++)
				{
					if (result[I - 1] == '(')
					{
						c++;
					}
					else
					{
						if (result[I - 1] == ')' || I == result.Length)
						{
							c--;
							if (c <= 0 || I == result.Length)
							{
								if (result[I - 1] == ')')
								{
									this.fDatePhrase = result.Substring(0, I - 1);
								}
								else
								{
									this.fDatePhrase = result.Substring(0, I);
								}
								result = result.Remove(0, I);
								break;
							}
						}
					}
				}
			}
			return result;
		}

		public override string ParseString(string strValue)
		{
			string result = strValue;
			if (!string.IsNullOrEmpty(result))
			{
				result = GEDCOMUtils.ExtractDelimiter(result, 0);
				if (result.Substring(0, 3).ToUpperInvariant() == "INT")
				{
					result = result.Remove(0, 3);
				}
				result = GEDCOMUtils.ExtractDelimiter(result, 0);
				result = base.ParseString(result);
				result = GEDCOMUtils.ExtractDelimiter(result, 0);
				result = this.ExtractPhrase(result);
			}
			return result;
		}

		public GEDCOMDateInterpreted(GEDCOMTree owner, GEDCOMObject parent, string tagName, string tagValue) : base(owner, parent, tagName, tagValue)
		{
		}
	}
}
