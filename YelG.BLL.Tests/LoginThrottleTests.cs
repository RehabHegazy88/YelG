using YelG.BLL.Common;

namespace YelG.BLL.Tests
{
    public class LoginThrottleTests
    {
        private sealed class ManualClock : TimeProvider
        {
            public DateTimeOffset Now { get; set; } = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

            public override DateTimeOffset GetUtcNow() => Now;
        }

        private readonly ManualClock _clock = new();

        private LoginThrottle NewThrottle(int perUser = 3, int perAddress = 5) =>
            new(new LoginThrottleOptions { MaxFailuresPerUser = perUser, MaxFailuresPerAddress = perAddress, Window = TimeSpan.FromMinutes(15) }, _clock);

        [Fact]
        public void A_user_name_is_blocked_after_the_limit_and_frees_up_as_failures_leave_the_window()
        {
            var throttle = NewThrottle();
            Assert.False(throttle.RecordFailure("10.0.0.1", "nobody"));
            _clock.Now = _clock.Now.AddMinutes(5);
            Assert.False(throttle.RecordFailure("10.0.0.2", "nobody"));
            Assert.Null(throttle.RetryAfter("10.0.0.3", "nobody"));

            Assert.True(throttle.RecordFailure("10.0.0.3", "NoBody "));   // same name, any case or spacing: starts the block
            Assert.Equal(TimeSpan.FromMinutes(10), throttle.RetryAfter("10.0.0.9", "nobody"));   // first failure expires at +15

            _clock.Now = _clock.Now.AddMinutes(10);
            Assert.Null(throttle.RetryAfter("10.0.0.9", "nobody"));
            Assert.Null(throttle.RetryAfter("10.0.0.1", "someone-else"));
        }

        [Fact]
        public void One_address_trying_many_names_is_blocked_but_other_addresses_are_not()
        {
            var throttle = NewThrottle(perUser: 100, perAddress: 5);
            for (var i = 0; i < 4; i++) Assert.False(throttle.RecordFailure("10.0.0.1", "user" + i));
            Assert.True(throttle.RecordFailure("10.0.0.1", "user4"));
            Assert.False(throttle.RecordFailure("10.0.0.1", "user5"));   // the block is reported once

            Assert.NotNull(throttle.RetryAfter("10.0.0.1", "fresh-name"));
            Assert.Null(throttle.RetryAfter("10.0.0.2", "fresh-name"));
            Assert.Null(throttle.RetryAfter(null, "fresh-name"));         // unknown address: only the name counts
        }

        [Fact]
        public void A_successful_sign_in_clears_the_name_but_not_the_address()
        {
            var throttle = NewThrottle(perUser: 2, perAddress: 3);
            throttle.RecordFailure("10.0.0.1", "reviewer");
            throttle.RecordFailure("10.0.0.1", "reviewer");
            Assert.NotNull(throttle.RetryAfter("10.0.0.2", "reviewer"));

            throttle.Reset("reviewer");
            Assert.Null(throttle.RetryAfter("10.0.0.2", "reviewer"));

            throttle.RecordFailure("10.0.0.1", "other");                  // third failure from this address
            Assert.NotNull(throttle.RetryAfter("10.0.0.1", "reviewer"));
        }
    }
}
