namespace DataStandardizer.Communication.E164
{
    /// <summary>
    /// Represents an ITU-T E.164 international telephone number specifically designated for trials.
    /// </summary>
    /// <remarks>
    /// This interface extends the base <see cref="IItuE164InternationalNumber"/> interface to include
    /// additional properties specific to trial numbers, such as the trial identification code and
    /// an optional subscriber number. It is intended for use in scenarios where experimental or
    /// trial numbering schemes are required.
    /// </remarks>
    public interface IItuE164InternationalNumberForTrials : IItuE164InternationalNumber
    {
        /// <summary>
        /// Gets the trial identification code associated with the ITU-T E.164 international telephone number for trials.
        /// </summary>
        /// <remarks>
        /// The trial identification code is a unique identifier used to distinguish trial numbers
        /// within the ITU-T E.164 numbering plan. This property is specific to numbers designated
        /// for experimental or trial purposes.
        /// </remarks>
        ItuE164AssignedTrialIdentificationCodesForTrials TrialIdentificationCode { get; }

        /// <summary>
        /// Gets the optional subscriber number associated with the ITU-T E.164 international telephone number for trials.
        /// </summary>
        /// <remarks>
        /// The subscriber number is an additional component of the trial-specific E.164 number, 
        /// which may be used to uniquely identify a subscriber within the context of a trial numbering scheme.
        /// This property is nullable, as not all trial numbers may include a subscriber number.
        /// </remarks>
        ItuE164SubscriberNumber? SubscriberNumber { get; }
    }
}