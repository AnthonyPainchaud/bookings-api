using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookings.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingOverlapExclusionConstraint : Migration
    {
        // This constraint cannot be expressed through the EF model, so it is
        // written as raw SQL. It guarantees, at the database level, that no two
        // non-cancelled bookings for the same resource can have overlapping time
        // ranges — making the guarantee race-proof regardless of how many
        // requests arrive concurrently.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // btree_gist lets a GiST index combine equality on a scalar column
            // (ResourceId) with the range-overlap operator on tstzrange.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            // tstzrange(StartsAt, EndsAt) defaults to '[)' bounds (half-open), so
            // back-to-back bookings (one ends exactly when the next starts) do NOT
            // overlap. Cancelled bookings are excluded via the WHERE predicate,
            // which frees the slot for rebooking.
            migrationBuilder.Sql(
                """
                ALTER TABLE bookings
                    ADD CONSTRAINT ex_bookings_no_overlap
                    EXCLUDE USING gist (
                        "ResourceId" WITH =,
                        tstzrange("StartsAt", "EndsAt") WITH &&
                    )
                    WHERE ("Status" <> 'Cancelled');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE bookings DROP CONSTRAINT IF EXISTS ex_bookings_no_overlap;");
            // The btree_gist extension is left in place; dropping it could affect
            // other objects and it is harmless to keep.
        }
    }
}
