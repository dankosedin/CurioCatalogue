use strict;
use warnings;
use utf8;
use open qw(:std :encoding(UTF-8));

# Usage: perl lang-table.pl en.json uk.json — prints Markdown tables of all texts, grouped like REVIEW.md.
my ($enFile, $ukFile) = @ARGV;

sub read_lang {
    my ($file) = @_;
    local $/;
    open my $in, '<', $file or die "$file: $!";
    my $text = <$in>;
    close $in;
    my (@order, %value);
    while ($text =~ /"([^"]+)":\s*"((?:[^"\\]|\\.)*)"/g) {
        push @order, $1;
        $value{$1} = $2;
    }
    return (\@order, \%value);
}

my ($order, $en) = read_lang($enFile);
my (undef, $uk) = read_lang($ukFile);

my @groups = (
    ['Назви предметів', qr/^(modname|item-|album-name-)/],
    ['Підказки предметів (тултіп)', qr/^(ledger-info-|invoice-info-|unknown-item|cover-|game:tapestry-)/],
    ['Довідник і гайд', qr/^(handbook-|item-handbook|guide-)/],
    ['Підказки над контейнером', qr/^heldhelp-/],
    ['Повідомлення в чаті (результат)', qr/^(invoice-made|invoice-posted|invoice-cancelled|ledger-overlimit|ledger-cleared|album-restarted|album-collected|wantlist-|notfitted)/],
    ['Червоні відмови', qr/^ingameerror-/],
    ['Вікна', qr/^(ledgerwindow-|invoicewindow-|confirm-)/],
);

sub clean {
    my ($s) = @_;
    $s //= '—';
    $s =~ s/\\n|\n/ ⏎ /g;
    $s =~ s/\|/\\|/g;
    return $s;
}

my %done;
for my $group (@groups) {
    my ($title, $re) = @$group;
    my @keys = grep { my $k = $_; $k =~ s/^curiocatalogue://; $k =~ $re } @$order;
    next unless @keys;
    print "### $title\n\n| Ключ | Українська | English | Нотатки |\n|---|---|---|---|\n";
    for my $key (@keys) {
        (my $short = $key) =~ s/^curiocatalogue://;
        print "| `$short` | ", clean($uk->{$key}), " | ", clean($en->{$key}), " | |\n";
        $done{$key} = 1;
    }
    print "\n";
}
my @rest = grep { !$done{$_} } @$order;
if (@rest) {
    print "### Інше\n\n| Ключ | Українська | English | Нотатки |\n|---|---|---|---|\n";
    for my $key (@rest) {
        (my $short = $key) =~ s/^curiocatalogue://;
        print "| `$short` | ", clean($uk->{$key}), " | ", clean($en->{$key}), " | |\n";
    }
    print "\n";
}
