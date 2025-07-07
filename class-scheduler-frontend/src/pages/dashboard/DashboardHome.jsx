const DashboardHome = () => {
  return (
    <div className="space-y-6">
      <h1 className="text-3xl font-bold text-primary">
        Welcome to PCNL Scheduler
      </h1>
      <p className="text-gray-600">
        This is your dashboard overview. Scroll down to test the layout.
      </p>

      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
        {Array.from({ length: 12 }, (_, i) => (
          <div
            key={i}
            className="bg-white shadow rounded-xl p-4 border border-gray-100"
          >
            <h2 className="text-lg font-semibold text-gray-800">
              Widget {i + 1}
            </h2>
            <p className="text-sm text-gray-500">
              Some placeholder content for demonstration purposes.
            </p>
          </div>
        ))}
      </div>

      <div className="h-[600px] bg-base-200 flex items-center justify-center rounded-xl">
        <p className="text-gray-500">Keep scrolling... 🎢</p>
      </div>
    </div>
  );
};

export default DashboardHome;
